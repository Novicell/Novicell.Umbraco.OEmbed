using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.Services;
using Novicell.Umbraco.OEmbed.Tests.TestSupport;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core;

namespace Novicell.Umbraco.OEmbed.Tests.Services;

[TestFixture]
public class OEmbedServiceTests
{
    private static readonly Uri ProviderEndpoint = new("https://provider.example/oembed");
    private static readonly OEmbedApiEndpoint ApiEndpoint = new("TestProvider", ProviderEndpoint);

    private IOEmbedApiEndpointProvider _endpointProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _endpointProvider = Substitute.For<IOEmbedApiEndpointProvider>();
    }

    private OEmbedService CreateService(HttpMessageHandler handler)
        => new(
            _endpointProvider,
            new StubHttpClientFactory(handler),
            NullLogger<OEmbedService>.Instance);

    private static OEmbedRequest Request(string url = "https://youtube.com/watch?v=123")
        => new() { Url = new Uri(url) };

    // --- Provider matching / fallthrough ---

    [Test]
    public async Task Uses_The_Endpoint_Of_The_Provider_That_Matches()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, ValidJsonResponse()));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Reports_ProviderNotSupported_And_Requests_Nothing_When_No_Provider_Matches()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns((OEmbedApiEndpoint?)null);
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("No request expected."));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.ProviderNotSupported));
    }

    // --- HTTP status -> operation status mapping ---

    [Test]
    public async Task Maps_BadRequest_To_InvalidResponse()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    [Test]
    public async Task Maps_NotFound_To_EmbedNotFound()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.EmbedNotFound));
    }

    [Test]
    public async Task Maps_Unsupported_ContentType_To_InvalidResponse()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("plain text") };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            return response;
        });
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    [Test]
    public async Task Unhandled_Exception_During_Fetch_Maps_To_InvalidResponse()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    // --- JSON parsing ---

    [Test]
    public async Task Parses_Valid_Json_Response()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, ValidJsonResponse()));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Result!.Type, Is.EqualTo(OEmbedType.Video));
        Assert.That(result.Result!.Title, Is.EqualTo("A test video"));
    }

    [Test]
    public async Task Photo_Type_Json_Response_Nulls_Out_Html()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        const string json = """{"type":"photo","version":"1.0","html":"<div>should be ignored by consumer</div>"}""";
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, json));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Result!.Type, Is.EqualTo(OEmbedType.Photo));
    }

    [Test]
    public async Task Malformed_Json_Maps_To_InvalidResponse_Without_Throwing()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{ not valid json"));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    [Test]
    public async Task Empty_Json_Body_Maps_To_InvalidResponse()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, string.Empty));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    // --- The provider's own object ---

    [Test]
    public async Task Json_Response_Keeps_Everything_The_Provider_Returned()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        const string json = """
            {"type":"video","version":"1.0","title":"A test video","width":"560","height":315,
             "thumbnail_url_with_play_button":"https://example.com/play.jpg","is_plus":1,"tags":["a"]}
            """;
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, json));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Result!.Extra, Is.Not.Null);
        Assert.That(result.Result.Extra!.Keys, Is.EquivalentTo(new[] { "thumbnail_url_with_play_button", "is_plus", "tags" }));
        Assert.That(result.Result.Extra["thumbnail_url_with_play_button"].GetString(), Is.EqualTo("https://example.com/play.jpg"));
        // The typed fields are read leniently: a width sent as text is still a number.
        Assert.That(result.Result.Width, Is.EqualTo(560));
        Assert.That(result.Result.Height, Is.EqualTo(315));
    }

    [Test]
    public async Task One_Field_Of_An_Unexpected_Kind_Does_Not_Fail_The_Json_Response()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        const string json = """{"type":"video","title":5,"width":"wide","author_url":["x"],"provider_name":"YouTube"}""";
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, json));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Result!.Title, Is.Null);
        Assert.That(result.Result.Width, Is.Null);
        Assert.That(result.Result.AuthorUrl, Is.Null);
        Assert.That(result.Result.ProviderName, Is.EqualTo("YouTube"));
    }

    [TestCase("""{"title":"no type"}""")]
    [TestCase("""{"type":"movie"}""")]
    [TestCase("""{"type":1}""")]
    [TestCase("""["type","video"]""")]
    [TestCase("""5""")]
    [TestCase("""null""")]
    public async Task Json_Response_Without_A_Valid_Type_Maps_To_InvalidResponse(string json)
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, json));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    // --- Only JSON is read ---

    [Test]
    public async Task Asks_The_Provider_For_Json()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, ValidJsonResponse()));
        OEmbedService service = CreateService(handler);

        await service.GetOEmbedAsync(Request(), CancellationToken.None);

        System.Collections.Specialized.NameValueCollection query =
            System.Web.HttpUtility.ParseQueryString(handler.LastRequest!.RequestUri!.Query);
        Assert.That(query["format"], Is.EqualTo("json"));
    }

    [Test]
    public async Task An_Endpoint_That_Asks_For_Xml_Is_Asked_For_Json_Instead()
    {
        var endpointForXml = new OEmbedApiEndpoint("TestProvider", new Uri("https://provider.example/oembed?format=xml&other=1"));
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(endpointForXml);
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, ValidJsonResponse()));
        OEmbedService service = CreateService(handler);

        await service.GetOEmbedAsync(Request(), CancellationToken.None);

        System.Collections.Specialized.NameValueCollection query =
            System.Web.HttpUtility.ParseQueryString(handler.LastRequest!.RequestUri!.Query);
        Assert.That(query.GetValues("format"), Is.EqualTo(new[] { "json" }));
        Assert.That(query["other"], Is.EqualTo("1"));
    }

    [Test]
    public async Task An_Xml_Response_Is_Not_Read()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        const string xml = "<?xml version=\"1.0\"?><oembed><type>video</type><title>x</title></oembed>";
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.XmlResponse(HttpStatusCode.OK, xml));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    [Test]
    public async Task A_Response_That_Is_Numbers_As_Text_And_A_Numeric_Version_Is_Read()
    {
        // SoundCloud sends the version as a number and TikTok sends the sizes as text.
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        const string json = """{"version":1.0,"type":"video","width":"325","height":"578"}""";
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, json));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result = await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Result!.Version, Is.EqualTo("1.0"));
        Assert.That(result.Result.Width, Is.EqualTo(325));
        Assert.That(result.Result.Height, Is.EqualTo(578));
    }

    // --- BuildEndpointUrl behaviour (exercised indirectly via the captured request) ---

    [Test]
    public async Task Appends_MaxWidth_And_MaxHeight_Only_When_Positive()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, ValidJsonResponse()));
        OEmbedService service = CreateService(handler);

        await service.GetOEmbedAsync(
            new OEmbedRequest { Url = new Uri("https://youtube.com/watch?v=123"), MaxWidth = 640, MaxHeight = 0 },
            CancellationToken.None);

        System.Collections.Specialized.NameValueCollection query =
            System.Web.HttpUtility.ParseQueryString(handler.LastRequest!.RequestUri!.Query);
        Assert.That(query["maxwidth"], Is.EqualTo("640"));
        Assert.That(query["maxheight"], Is.Null);
    }

    [Test]
    public async Task Preserves_Existing_Query_Parameters_On_Provider_Endpoint()
    {
        var endpointWithQuery = new OEmbedApiEndpoint("TestProvider", new Uri("https://provider.example/oembed?format=json"));
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(endpointWithQuery);
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, ValidJsonResponse()));
        OEmbedService service = CreateService(handler);

        await service.GetOEmbedAsync(Request(), CancellationToken.None);

        System.Collections.Specialized.NameValueCollection query =
            System.Web.HttpUtility.ParseQueryString(handler.LastRequest!.RequestUri!.Query);
        Assert.That(query["format"], Is.EqualTo("json"));
        Assert.That(query["url"], Is.Not.Null);
    }

    // --- Limits on what is requested and how much is read ---

    [TestCase("ftp://example.com/video")]
    [TestCase("file:///etc/passwd")]
    [TestCase("javascript:alert(1)")]
    public async Task Rejects_A_Request_Url_That_Is_Not_Http_Or_Https_Without_Looking_For_A_Provider(string url)
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("No request expected."));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result =
            await service.GetOEmbedAsync(Request(url), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.ProviderNotSupported));
        await _endpointProvider.DidNotReceive().GetApiEndpointAsync(Arg.Any<Uri>());
    }

    [Test]
    public async Task A_Provider_Lookup_That_Throws_Is_ProviderNotSupported_And_Requests_Nothing()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>())
            .Returns<Task<OEmbedApiEndpoint?>>(_ => throw new UriFormatException("Bad ApiEndpoint."));
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("No request expected."));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result =
            await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.ProviderNotSupported));
    }

    [Test]
    public async Task Json_Response_Larger_Than_The_Limit_Maps_To_InvalidResponse()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var tooLarge = ValidJsonResponse() + new string(' ', (int)OEmbedHttp.MaxResponseBytes);
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse(HttpStatusCode.OK, tooLarge));
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result =
            await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    [Test]
    public async Task Json_Response_Of_Unknown_Length_That_Turns_Out_Too_Large_Maps_To_InvalidResponse()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var tooLarge = ValidJsonResponse() + new string(' ', (int)OEmbedHttp.MaxResponseBytes);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new UnknownLengthContent(System.Text.Encoding.UTF8.GetBytes(tooLarge), "application/json"),
        });
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result =
            await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(OEmbedOperationStatus.InvalidResponse));
    }

    [Test]
    public async Task Json_Response_Just_Under_The_Limit_Is_Accepted()
    {
        _endpointProvider.GetApiEndpointAsync(Arg.Any<Uri>()).Returns(ApiEndpoint);
        var json = ValidJsonResponse();
        var padded = json + new string(' ', (int)OEmbedHttp.MaxResponseBytes - json.Length);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new UnknownLengthContent(System.Text.Encoding.UTF8.GetBytes(padded), "application/json"),
        });
        OEmbedService service = CreateService(handler);

        Attempt<OEmbedResponse?, OEmbedOperationStatus> result =
            await service.GetOEmbedAsync(Request(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    private static string ValidJsonResponse() =>
        """{"type":"video","version":"1.0","title":"A test video","html":"<iframe src=\"https://example.com\"></iframe>"}""";
}
