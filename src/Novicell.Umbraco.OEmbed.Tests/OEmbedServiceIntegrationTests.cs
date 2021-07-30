using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Novicell.Umbraco.OEmbed.Configuration.Models;
using Novicell.Umbraco.OEmbed.Media;
using Novicell.Umbraco.OEmbed.Services;
using Umbraco.Cms.Core.Media;
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Infrastructure.Serialization;
using Xunit;

namespace Novicell.Umbraco.OEmbed.Tests
{
    [Trait("Category", "Services")]
    [Trait("Category", "Integration")]
    public class OEmbedServiceIntegrationTests
    {
        private readonly EmbedProvidersCollection YouTubeOnlyEmbedProvidersCollection;
        private readonly EmbedProvidersCollection SoundCloudOnlyEmbedProvidersCollection;
        private readonly EmbedProvidersCollection EmptyEmbedProvidersCollection;
        private readonly IHttpClientFactory _httpClientFactory = new HttpClientFactory();

        public OEmbedServiceIntegrationTests()
        {
            var jsonSerializer = new JsonNetSerializer();

            YouTubeOnlyEmbedProvidersCollection = new EmbedProvidersCollection(new[]
            {
                new YouTube(jsonSerializer),
            });
            
            SoundCloudOnlyEmbedProvidersCollection = new EmbedProvidersCollection(new[]
            {
                new Soundcloud(jsonSerializer),
            });

            EmptyEmbedProvidersCollection = new EmbedProvidersCollection(Array.Empty<IEmbedProvider>());
        }

        [Fact]
        public async Task EmbedSoundcloudRichFromKnownProvider()
        {
            var oembed = new OEmbedService(SoundCloudOnlyEmbedProvidersCollection, _httpClientFactory, null, null);
            var embed = await oembed.GetOEmbedAsync(new Uri("https://soundcloud.com/dj_zanderz/mellemfingamuzik-jungle-zanderz-remix-demo"), 0, 0);
            Assert.True(embed.Success);
            Assert.NotNull(embed.Result);
            Assert.StartsWith("MellemFingaMuzik", embed.Result.Title);
        }

        [Fact]
        public async Task EmbedYouTubeVideoFromKnownProvider()
        {
            var oembed = new OEmbedService(YouTubeOnlyEmbedProvidersCollection, _httpClientFactory, null,null);
            var embed = await oembed.GetOEmbedAsync(new Uri("https://www.youtube.com/watch?v=dQw4w9WgXcQ"), 0, 0);
            Assert.True(embed.Success);
            Assert.NotNull(embed.Result);
            Assert.StartsWith("Rick Astley", embed.Result.Title);
        }

        [Fact]
        public async Task EmbedYouTubeVideoFromAutodiscover()
        {
            var settings = new OptionsWrapper<OEmbedSettings>(new OEmbedSettings
            {
                Autodiscover = true,
            });

            var discovery = new OEmbedDiscoveryService(_httpClientFactory);
            var oembed = new OEmbedService(EmptyEmbedProvidersCollection, _httpClientFactory, discovery, settings);
            var embed = await oembed.GetOEmbedAsync(new Uri("https://www.youtube.com/watch?v=dQw4w9WgXcQ"), 0, 0);
            Assert.True(embed.Success);
            Assert.NotNull(embed.Result);
            Assert.StartsWith("Rick Astley", embed.Result.Title);
        }

        [Fact]
        public async Task EmbedVimeoVideoFromAutodiscover()
        {
            var settings = new OptionsWrapper<OEmbedSettings>(new OEmbedSettings
            {
                Autodiscover = true,
            });

            var discovery = new OEmbedDiscoveryService(_httpClientFactory);
            var oembed = new OEmbedService(EmptyEmbedProvidersCollection, _httpClientFactory, discovery, settings);
            var embed = await oembed.GetOEmbedAsync(new Uri("https://vimeo.com/251769074"), 0, 0);
            Assert.True(embed.Success);
            Assert.NotNull(embed.Result);
            Assert.StartsWith("Welcome to Umbraco HQ", embed.Result.Title);

            if (embed.Result.TryGetValue<string>("upload_date", out var uploadDateValue) && 
                DateTime.TryParse(uploadDateValue, out var uploadDate))
            {
                /*Assert.NotNull(uploadDate);*/
                Assert.NotEqual(default, uploadDate);
            }
        }

        [Fact]
        public async Task FailWhenNoProvidersEnabled()
        {
            var settings = new OptionsWrapper<OEmbedSettings>(new OEmbedSettings
            {
                Autodiscover = false,
            });

            var oembed = new OEmbedService(EmptyEmbedProvidersCollection, _httpClientFactory, null, settings);

            var embed = await oembed.GetOEmbedAsync(new Uri("https://www.youtube.com/watch?v=0BPL5tT9_2Y"), 0, 0);
            Assert.False(embed.Success);
            Assert.IsType<OEmbedUrlNotSupportedException>(embed.Exception);
        }

        [Fact]
        public async Task FailWhenNoProvidersGiven()
        {
            var oembed = new OEmbedService(null, _httpClientFactory, null, null);

            var embed = await oembed.GetOEmbedAsync(new Uri("https://www.youtube.com/watch?v=0BPL5tT9_2Y"), 0, 0);
            Assert.False(embed.Success);
            Assert.IsType<OEmbedUrlNotSupportedException>(embed.Exception);
        }
    }
}
