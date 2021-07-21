using System;
using Novicell.Umbraco.OEmbed.Media;
using Novicell.Umbraco.OEmbed.Services;
using Xunit;

namespace Novicell.Umbraco.OEmbed.Tests
{
    [Trait("Category", "Services")]
    [Trait("Category", "Providers")]
    public class AutodiscoverEmbedProviderTests
    {
        [Fact]
        public void CanAutodiscoverParameters()
        {
            var url = new Uri("http://example.com/?a=1&b=2");

            var provider = new OEmbedDiscoveryService.AutodiscoverEmbedProvider(url);

            Assert.NotNull(provider);
            Assert.Equal("http://example.com/", provider.ApiEndpoint);

            Assert.NotEmpty(provider.RequestParams);
            Assert.Contains(provider.RequestParams, x => x.Key == "a");
            Assert.Contains(provider.RequestParams, x => x.Key == "b");

            Assert.Equal("1", provider.RequestParams["a"]);
            Assert.Equal("2", provider.RequestParams["b"]);
        }
    }
}