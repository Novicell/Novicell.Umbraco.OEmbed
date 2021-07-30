using System;
using System.Threading.Tasks;
using Novicell.Umbraco.OEmbed.Services;
using Xunit;
using static System.Net.WebRequestMethods;

namespace Novicell.Umbraco.OEmbed.Tests
{
    [Trait("Category", "Services")]
    public class OEmbedServiceUnitTests
    {
        [Fact]
        public async Task EmbedFromVimeoJson()
        {
            const string json = "{\"type\":\"video\",\"version\":\"1.0\",\"provider_name\":\"Vimeo\",\"provider_url\":\"https:\\/\\/vimeo.com\\/\",\"title\":\"Welcome to Umbraco HQ\",\"author_name\":\"Umbraco\",\"author_url\":\"https:\\/\\/vimeo.com\\/umbraco\",\"is_plus\":\"0\",\"account_type\":\"pro\",\"html\":\"<iframe src=\\\"https:\\/\\/player.vimeo.com\\/video\\/251769074?app_id=122963\\\" width=\\\"640\\\" height=\\\"360\\\" frameborder=\\\"0\\\" allow=\\\"autoplay; fullscreen; picture-in-picture\\\" allowfullscreen title=\\\"Welcome to Umbraco HQ\\\"><\\/iframe>\",\"width\":640,\"height\":360,\"duration\":210,\"description\":\"Niels Hartvig, the Umbraco founder with the prestigious title; Cheif Unicorn, will show you around Umbraco HQ.\",\"thumbnail_url\":\"https:\\/\\/i.vimeocdn.com\\/video\\/678231625_640\",\"thumbnail_width\":640,\"thumbnail_height\":360,\"thumbnail_url_with_play_button\":\"https:\\/\\/i.vimeocdn.com\\/filter\\/overlay?src0=https%3A%2F%2Fi.vimeocdn.com%2Fvideo%2F678231625_640&src1=http%3A%2F%2Ff.vimeocdn.com%2Fp%2Fimages%2Fcrawler_play.png\",\"upload_date\":\"2018-01-19 02:45:06\",\"video_id\":251769074,\"uri\":\"\\/videos\\/251769074\"}";

            var response = OEmbedService.DeserializeResponse<Models.OEmbedResponse>(json);
            Assert.NotNull(response);

            Assert.True(response.TryGetValue("account_type", out string accountType));
            Assert.Equal("pro", accountType);
            
            Assert.True(response.TryGetValue("duration", out int duration));
            Assert.Equal(210, duration);

            Assert.True(response.TryGetValue("upload_date", out DateTime uploadDate));
            Assert.Equal(2018, uploadDate.Year);
            Assert.Equal(1, uploadDate.Month);
            Assert.Equal(19, uploadDate.Day);


            await Task.CompletedTask;
        }

        [Fact]
        public async Task EmbedFromExampleJson()
        {
            // example from https://oembed.com/#section1

            const string json = "{\"version\": \"1.0\",\"type\": \"photo\",\"width\": 240,\"height\": 160,\"title\": \"ZB8T0193\",\"url\": \"http://farm4.static.flickr.com/3123/2341623661_7c99f48bbf_m.jpg\",	\"author_name\": \"Bees\",\"author_url\": \"http://www.flickr.com/photos/bees/\",\"provider_name\": \"Flickr\",\"provider_url\": \"http://www.flickr.com/\"}";

            var response = OEmbedService.DeserializeResponse<Models.OEmbedResponse>(json);
            Assert.NotNull(response);

            Assert.Equal("photo", response.Type);
            Assert.Equal(240, response.Width);
            Assert.Equal(160, response.Height);

            await Task.CompletedTask;
        }

        [Fact]
        public async Task EmbedFromExampleXml()
        {
            // example from https://oembed.com/#section5

            const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><oembed><version>1.0</version><type>link</type><author_name>Cal Henderson</author_name><author_url>http://iamcal.com/</author_url><cache_age>86400</cache_age><provider_name>iamcal.com</provider_name><provider_url>http://iamcal.com/</provider_url></oembed>";

            var json = OEmbedService.ConvertXmlToJson(xml);
            Assert.NotNull(json);

            var response = OEmbedService.DeserializeResponse<Models.OEmbedResponse>(json);
            Assert.NotNull(response);

            Assert.Equal("link", response.Type);

            Assert.Equal("Cal Henderson", response.AuthorName);
            Assert.Equal("http://iamcal.com/", response.AuthorUrl);

            Assert.Equal("iamcal.com", response.ProviderName);
            Assert.Equal("http://iamcal.com/", response.ProviderUrl);

            Assert.Equal(86400, response.CacheAge);

            await Task.CompletedTask;
        }
    }
}
