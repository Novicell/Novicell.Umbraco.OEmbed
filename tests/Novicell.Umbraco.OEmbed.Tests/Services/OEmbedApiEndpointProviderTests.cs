using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.Services;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Media;
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Serialization;

namespace Novicell.Umbraco.OEmbed.Tests.Services;

[TestFixture]
public class OEmbedApiEndpointProviderTests
{
    [TestCase("https://vimeo.com/api/oembed.xml", "https://vimeo.com/api/oembed.json")]
    [TestCase("http://www.ted.com/talks/oembed.xml", "http://www.ted.com/talks/oembed.json")]
    [TestCase("https://vimeo.com/api/oembed.XML", "https://vimeo.com/api/oembed.json")]
    [TestCase("https://vimeo.com/api/oembed.xml?width=100", "https://vimeo.com/api/oembed.json?width=100")]
    [TestCase("https://www.youtube.com/oembed", "https://www.youtube.com/oembed")]
    [TestCase("http://www.hulu.com/api/oembed.json", "http://www.hulu.com/api/oembed.json")]
    [TestCase("https://example.com/xml/oembed", "https://example.com/xml/oembed")]
    [TestCase("https://example.com/oembed?format=a.xml", "https://example.com/oembed?format=a.xml")]
    public void An_Endpoint_That_Is_Xml_By_Its_Path_Is_Changed_To_Json(string endpoint, string expected)
        => Assert.That(OEmbedApiEndpointProvider.ToJsonEndpoint(new Uri(endpoint)), Is.EqualTo(new Uri(expected)));

    [Test]
    public async Task Vimeo_Is_Asked_At_Its_Json_Endpoint()
    {
        var vimeo = new Vimeo(Substitute.For<IJsonSerializer>());
        var provider = new OEmbedApiEndpointProvider(new EmbedProvidersCollection(() => [vimeo]));

        OEmbedApiEndpoint? endpoint = await provider.GetApiEndpointAsync(new Uri("https://vimeo.com/371827688"));

        Assert.That(endpoint, Is.Not.Null);
        Assert.That(endpoint!.Endpoint.AbsolutePath, Does.EndWith("/oembed.json"));
    }

    [Test]
    public async Task The_Endpoint_Is_Named_After_The_Provider()
    {
        var vimeo = new Vimeo(Substitute.For<IJsonSerializer>());
        var provider = new OEmbedApiEndpointProvider(new EmbedProvidersCollection(() => [vimeo]));

        OEmbedApiEndpoint? endpoint = await provider.GetApiEndpointAsync(new Uri("https://vimeo.com/371827688"));

        Assert.That(endpoint?.Name, Is.EqualTo(nameof(Vimeo)));
    }

    [Test]
    public async Task The_Request_Params_Of_A_Provider_Are_Encoded_Once()
    {
        var withParams = new ProviderWithParams(Substitute.For<IJsonSerializer>());
        var provider = new OEmbedApiEndpointProvider(new EmbedProvidersCollection(() => [withParams]));

        OEmbedApiEndpoint? endpoint = await provider.GetApiEndpointAsync(new Uri("https://example.com/video/1"));

        Assert.That(endpoint, Is.Not.Null);
        Assert.That(endpoint!.Endpoint.Query, Does.Contain("theme=dark+%26+light"));
        Assert.That(System.Web.HttpUtility.ParseQueryString(endpoint.Endpoint.Query)["theme"], Is.EqualTo("dark & light"));
    }

    private sealed class ProviderWithParams(IJsonSerializer jsonSerializer) : OEmbedProviderBase(jsonSerializer)
    {
        public override string ApiEndpoint => "https://example.com/oembed";

        public override string[] UrlSchemeRegex => [@"^https:\/\/example\.com\/video\/"];

        public override Dictionary<string, string> RequestParams => new() { ["theme"] = "dark & light" };

        public override Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }

    [Test]
    public void None_Of_Umbracos_Providers_Is_Left_With_An_Xml_Endpoint()
    {
        var providers = typeof(Vimeo).Assembly.GetTypes()
            .Where(t => typeof(IEmbedProvider).IsAssignableFrom(t) && t is { IsAbstract: false, IsPublic: true, Namespace: "Umbraco.Cms.Core.Media.EmbedProviders" })
            .Select(t => (IEmbedProvider)Activator.CreateInstance(t, [Substitute.For<IJsonSerializer>()])!)
            .ToList();

        Assert.That(providers, Is.Not.Empty);

        foreach (IEmbedProvider provider in providers)
        {
            Uri endpoint = OEmbedApiEndpointProvider.ToJsonEndpoint(new Uri(provider.ApiEndpoint));

            Assert.That(endpoint.AbsolutePath, Does.Not.EndWith(".xml").IgnoreCase, provider.GetType().Name);
        }
    }
}
