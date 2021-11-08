using Novicell.Umbraco.OEmbed.Core.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Novicell.Umbraco.OEmbed.Tests
{
    [Trait("Category", "Services")]
    [Trait("Category", "Integration")]
    public class OEmbedDiscoveryService_DiscoverFromUrl_Tests
    {
        private readonly IHttpClientFactory _httpClientFactory = new HttpClientFactory();

        [Fact]
        public async void CanNotDiscoverInvalidUrl()
        {
            var url = new Uri("https://0.0.0.0/not-found");

            var discovery = new OEmbedDiscoveryService(_httpClientFactory);

            var provider = await discovery.DiscoverFromUrlAsync(url);

            Assert.False(provider.Success);
            Assert.Null(provider.Result);
        }

        [Fact]
        public async Task CanDiscoverFlickrEndpoint()
        {
            var url = new Uri("https://www.flickr.com/photos/percipientstudios/51505498075");

            var discovery = new OEmbedDiscoveryService(_httpClientFactory);

            var provider = await discovery.DiscoverFromUrlAsync(url);

            Assert.True(provider.Success);
            Assert.NotNull(provider.Result);
            Assert.Equal("https://www.flickr.com/services/oembed", provider.Result.ApiEndpoint);

            Assert.StartsWith(provider.Result.RequestParams["url"], url.AbsoluteUri);
        }

        [Fact]
        public async Task CanDiscoverVimeoEndpoint()
        {
            var url = new Uri("https://vimeo.com/371827688");

            var discovery = new OEmbedDiscoveryService(_httpClientFactory);

            var provider = await discovery.DiscoverFromUrlAsync(url);

            Assert.True(provider.Success);
            Assert.NotNull(provider.Result);

            Assert.Equal("https://vimeo.com/api/oembed.json", provider.Result.ApiEndpoint);

            // use StartsWith, as Vimeo appends some data to the url in the url-param
            Assert.StartsWith(url.AbsoluteUri, provider.Result.RequestParams["url"]);
        }

        [Fact]
        public async Task CanDiscoverYoutubeEndpoint()
        {
            var url = new Uri("https://www.youtube.com/watch?v=dQw4w9WgXcQ");

            var discovery = new OEmbedDiscoveryService(_httpClientFactory);

            var provider = await discovery.DiscoverFromUrlAsync(url);

            Assert.True(provider.Success);
            Assert.NotNull(provider.Result);

            Assert.Equal("https://www.youtube.com/oembed", provider.Result.ApiEndpoint);

            Assert.Equal("json", provider.Result.RequestParams["format"]);
            Assert.Equal(url.AbsoluteUri, provider.Result.RequestParams["url"]);
        }
    }
}