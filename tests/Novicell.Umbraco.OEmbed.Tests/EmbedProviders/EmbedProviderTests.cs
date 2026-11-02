using System.Text.RegularExpressions;
using Novicell.Umbraco.OEmbed.EmbedProviders;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Media;
using Umbraco.Cms.Core.Serialization;

namespace Novicell.Umbraco.OEmbed.Tests.EmbedProviders;

[TestFixture]
public class EmbedProviderTests
{
    private static IEnumerable<TestCaseData> Providers()
    {
        yield return new TestCaseData(typeof(Reddit), "https://www.reddit.com/r/umbraco/comments/abc123/a_post/");
        yield return new TestCaseData(typeof(Spotify), "https://open.spotify.com/track/7ouMYWpwJ422jRcDASZB7P");
        yield return new TestCaseData(typeof(TikTok), "https://www.tiktok.com/@scout2015/video/6718335390845095173");
    }

    // A site that registers the package by hand, or builds its own list of providers, adds these itself.
    [TestCaseSource(nameof(Providers))]
    public void A_Provider_Is_Public_So_A_Site_Can_Register_It(Type type, string _)
        => Assert.That(type.IsPublic && typeof(IEmbedProvider).IsAssignableFrom(type), Is.True, type.Name);

    [TestCaseSource(nameof(Providers))]
    public void A_Provider_Matches_A_Url_Of_Its_Platform(Type type, string url)
    {
        var provider = (IEmbedProvider)Activator.CreateInstance(type, Substitute.For<IJsonSerializer>())!;

        Assert.That(provider.UrlSchemeRegex.Any(pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase)), Is.True);
    }
}
