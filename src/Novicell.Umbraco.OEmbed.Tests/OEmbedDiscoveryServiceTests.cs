using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Novicell.Umbraco.OEmbed.Media;
using Novicell.Umbraco.OEmbed.Services;
using Xunit;

namespace Novicell.Umbraco.OEmbed.Tests
{
    [Trait("Category", "Services")]
    [Trait("Category", "Integration")]
    public class OEmbedDiscoveryServiceTests
    {
        private readonly ILogger<OEmbedDiscoveryService> _logger = new NullLogger<OEmbedDiscoveryService>();
        private readonly IHttpClientFactory _httpClientFactory = new HttpClientFactory();

        [Fact]
        public async Task CanNotDiscoverInvalidUrl()
        {
            var url = new Uri("https://0.0.0.0/not-found");

            var discovery = new OEmbedDiscoveryService(_httpClientFactory, _logger);

            var provider = await discovery.DiscoverFromUrlAsync(url);

            Assert.False(provider.Success);
            Assert.Null(provider.Result);
        }

        [Fact]
        public async Task CanDiscoverFlickrEndpoint()
        {
            var url = new Uri("https://www.flickr.com/photos/from-reinier/51234040000/in/explore-2021-06-09/");

            var discovery = new OEmbedDiscoveryService(_httpClientFactory, _logger);

            var provider = await discovery.DiscoverFromUrlAsync(url);

            Assert.True(provider.Success);
            Assert.NotNull(provider.Result);
            Assert.Equal("https://www.flickr.com/services/oembed", provider.Result.ApiEndpoint);

            Assert.StartsWith(provider.Result.RequestParams["url"], url.AbsoluteUri);
        }

        [Fact]
        public async Task CanDiscoverVimeoEndpoint()
        {
            var url = new Uri("https://vimeo.com/302232607");

            var discovery = new OEmbedDiscoveryService(_httpClientFactory, _logger);

            var provider = await discovery.DiscoverFromUrlAsync(url);

            Assert.True(provider.Success);
            Assert.NotNull(provider.Result);

            Assert.Equal("https://vimeo.com/api/oembed.json", provider.Result.ApiEndpoint);

            Assert.Equal(url.AbsoluteUri, provider.Result.RequestParams["url"]);
        }

        [Fact]
        public async Task CanDiscoverYoutubeEndpoint()
        {
            var url = new Uri("https://www.youtube.com/watch?v=vcWehsxBLzs");

            var discovery = new OEmbedDiscoveryService(_httpClientFactory, _logger);

            var provider = await discovery.DiscoverFromUrlAsync(url);

            Assert.True(provider.Success);
            Assert.NotNull(provider.Result);

            Assert.Equal("https://www.youtube.com/oembed", provider.Result.ApiEndpoint);

            Assert.Equal(url.AbsoluteUri, provider.Result.RequestParams["url"]);
        }
    }
}