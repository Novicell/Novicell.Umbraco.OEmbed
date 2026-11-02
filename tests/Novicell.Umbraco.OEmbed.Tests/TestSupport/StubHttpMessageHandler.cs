namespace Novicell.Umbraco.OEmbed.Tests.TestSupport;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that returns a fixed response and records the last request made to it.
/// </summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(respond(request));
    }

    public static HttpResponseMessage JsonResponse(System.Net.HttpStatusCode statusCode, string json)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json)
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return response;
    }

    public static HttpResponseMessage XmlResponse(System.Net.HttpStatusCode statusCode, string xml)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(xml)
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");
        return response;
    }
}
