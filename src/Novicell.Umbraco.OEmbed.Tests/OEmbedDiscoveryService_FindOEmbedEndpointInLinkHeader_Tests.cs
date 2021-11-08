using Novicell.Umbraco.OEmbed.Core.Services;
using System;
using Xunit;

namespace Novicell.Umbraco.OEmbed.Tests
{
    [Trait("Category", "Services")]
    public class OEmbedDiscoveryService_FindOEmbedEndpointInLinkHeader_Tests
    {
        [Fact]
        public void CanFindAlternateOEmbedUrlFromLinkHeader()
        {
            const string linkHeaderValue = "<https://example.com/oembed/>; rel=alternate; type=application/json+oembed";

            var endpoint = OEmbedDiscoveryService.FindOEmbedEndpointInLinkHeader(new[]
            {
                linkHeaderValue
            });

            Assert.NotNull(endpoint);
            Assert.Equal(new Uri("https://example.com/oembed/"), endpoint);
        }

        [Fact]
        public void CanNotFindAlternateOEmbedUrlFromLinkHeaderWithoutOEmbedType()
        {
            const string linkHeaderValue = "<https://example.com/oembed/>; rel=alternate; type=not-text/not-json";

            var endpoint = OEmbedDiscoveryService.FindOEmbedEndpointInLinkHeader(new[]
            {
                linkHeaderValue
            });

            Assert.Null(endpoint);
        }
    }
}