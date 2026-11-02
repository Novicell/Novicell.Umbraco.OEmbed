namespace Novicell.Umbraco.OEmbed.Tests.TestSupport;

/// <summary>
/// An <see cref="IHttpClientFactory"/> that always returns an <see cref="HttpClient"/> backed by a fixed handler.
/// </summary>
internal sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
