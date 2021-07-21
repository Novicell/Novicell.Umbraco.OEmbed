using System.Net.Http;

namespace Novicell.Umbraco.OEmbed.Tests
{
    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}