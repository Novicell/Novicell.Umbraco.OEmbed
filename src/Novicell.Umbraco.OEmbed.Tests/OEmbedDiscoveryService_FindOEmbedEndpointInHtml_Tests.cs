using Novicell.Umbraco.OEmbed.Core.Services;
using System;
using Xunit;

namespace Novicell.Umbraco.OEmbed.Tests
{
    [Trait("Category", "Services")]
    public class OEmbedDiscoveryService_FindOEmbedEndpointInHtml_Tests
    {
        [Fact]
        public void CanOEmbedEndpointFromHtml()
        {
            const string html = "<html><head><link href=\"https://example.com/oembed/\" rel=\"alternate\" type=\"application/json+oembed\" /></head><body></body></html>";

            var endpoint = OEmbedDiscoveryService.FindOEmbedEndpointInHtml(html);

            Assert.NotNull(endpoint);
            Assert.Equal(new Uri("https://example.com/oembed/"), endpoint);
        }

        [Fact]
        public void CanNotOEmbedEndpointFromHtml()
        {
            const string html = "<html><head><link href=\"https://example.com/oembed/\" rel=\"random\" type=\"ran/dom+oembed\" /></head><body></body></html>";

            var endpoint = OEmbedDiscoveryService.FindOEmbedEndpointInHtml(html);

            Assert.Null(endpoint);
        }
    }
}