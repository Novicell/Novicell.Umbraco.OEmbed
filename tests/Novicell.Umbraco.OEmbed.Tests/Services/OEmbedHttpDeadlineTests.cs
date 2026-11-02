using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.Services;
using Novicell.Umbraco.OEmbed.Tests.TestSupport;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core;

namespace Novicell.Umbraco.OEmbed.Tests.Services;

/// <summary>
/// HttpClient.Timeout stops applying once the response headers have arrived (ResponseHeadersRead), so
/// a server that sends headers and then stalls the body must still be cut off by the deadline. Changes
/// the shared timeout, so the tests in this fixture cannot run in parallel with others.
/// </summary>
[TestFixture]
[NonParallelizable]
public class OEmbedHttpDeadlineTests
{
    private TimeSpan _originalTimeout;

    // Captured once, and restored when the fixture ends too, so a test that is aborted by its
    // [Timeout] cannot leave the short timeout behind for the tests that run after it.
    [OneTimeSetUp]
    public void OneTimeSetUp() => _originalTimeout = OEmbedHttp.Timeout;

    [OneTimeTearDown]
    public void OneTimeTearDown() => OEmbedHttp.Timeout = _originalTimeout;

    [SetUp]
    public void SetUp() => OEmbedHttp.Timeout = TimeSpan.FromMilliseconds(300);

    [TearDown]
    public void TearDown() => OEmbedHttp.Timeout = _originalTimeout;

    private static HttpResponseMessage Stalling(string mediaType, string prefix = "")
    {
        var content = new StreamContent(new StallingStream(Encoding.UTF8.GetBytes(prefix)));
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static OEmbedService CreateService(HttpMessageHandler handler)
    {
        IOEmbedApiEndpointProvider endpointProvider = Substitute.For<IOEmbedApiEndpointProvider>();
        endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>())
            .Returns(new OEmbedApiEndpoint("Test", new Uri("https://provider.example/oembed")));

        return new OEmbedService(
            endpointProvider,
            new StubHttpClientFactory(handler),
            NullLogger<OEmbedService>.Instance);
    }

    [Test, Timeout(5000)]
    public void Reading_A_Body_That_Stalls_Is_Cut_Off_By_The_Deadline()
    {
        using HttpResponseMessage response = Stalling("text/plain");
        using CancellationTokenSource deadline = OEmbedHttp.CreateDeadline(CancellationToken.None);

        Assert.ThrowsAsync<TaskCanceledException>(
            async () => await OEmbedHttp.ReadLimitedStringAsync(response.Content, deadline.Token));
    }

    [Test, Timeout(5000)]
    public async Task A_Json_Response_That_Stalls_After_The_Headers_Maps_To_InvalidResponse()
    {
        var handler = new StubHttpMessageHandler(_ => Stalling("application/json", "{\"type\":\"video\",\"title\":\"x"));

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await CreateService(handler)
            .GetOEmbedAsync(new OEmbedRequest { Url = new Uri("https://youtube.com/watch?v=1") }, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    [Test, Timeout(5000)]
    public async Task An_Xml_Response_That_Stalls_After_The_Headers_Maps_To_InvalidResponse()
    {
        // XmlReader reads the underlying stream without a cancellation token.
        var handler = new StubHttpMessageHandler(_ => Stalling("text/xml", "<oembed><type>video</type>"));

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await CreateService(handler)
            .GetOEmbedAsync(new OEmbedRequest { Url = new Uri("https://youtube.com/watch?v=1") }, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    [Test, Timeout(5000)]
    public async Task A_Caller_Cancelling_Still_Stops_The_Request()
    {
        var handler = new StubHttpMessageHandler(_ => Stalling("application/json", "{"));
        using var caller = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        OEmbedHttp.Timeout = TimeSpan.FromSeconds(30);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await CreateService(handler)
            .GetOEmbedAsync(new OEmbedRequest { Url = new Uri("https://youtube.com/watch?v=1") }, caller.Token);

        Assert.That(result.Success, Is.False);
    }
}
