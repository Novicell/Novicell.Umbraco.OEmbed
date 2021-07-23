using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Novicell.Umbraco.OEmbed.Services
{
    internal abstract class OEmbedServiceBase
    {
        /*
        private IHttpClientFactory _httpClientFactory;

        protected OEmbedServiceBase(IHttpClientFactory httpHttpClientFactory)
        {
            _httpClientFactory = httpHttpClientFactory;
        }

        protected async Task<(HttpStatusCode, MediaTypeHeaderValue, string)> GetHttpContentAsync(Uri url)
        {
            var client = _httpClientFactory.CreateClient();

            using var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (response.StatusCode, null, null);
            }

            response.EnsureSuccessStatusCode();

            return (response.StatusCode, response.Content.Headers.ContentType, await response.Content.ReadAsStringAsync());
        }
        */
    }
}