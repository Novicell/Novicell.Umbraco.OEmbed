using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Novicell.Umbraco.OEmbed.Tests.PropertyEditors;

/// <summary>
/// What a site sees is whatever <see cref="OEmbedPickerPropertyValueConverter"/> produces from the stored
/// JSON, and the stored JSON does not only come through the save validators: it is also written by
/// uSync, by a direct database change, or by an older version. The converter is therefore what makes
/// sure a site is never handed a script-capable URL.
/// </summary>
[TestFixture]
public class OEmbedPublishedValueSafetyTests
{
    private readonly OEmbedPickerPropertyValueConverter _converter = new();
    private readonly IPublishedElement _owner = Substitute.For<IPublishedElement>();

    private List<OEmbedValue> Convert(string json)
    {
        IPublishedPropertyType propertyType = Substitute.For<IPublishedPropertyType>();
        propertyType.EditorAlias.Returns(Constants.PropertyEditors.OEmbedPicker);

        return ((IEnumerable<OEmbedValue>)_converter.ConvertSourceToIntermediate(_owner, propertyType, json, false)!).ToList();
    }

    private static string Embed(string oembedProperties, string url = "https://youtube.com/watch?v=1")
        => $$"""[{ "url": "{{url}}", "oembed": { "type": "rich", {{oembedProperties}} } }]""";



    [TestCase("author_url")]
    [TestCase("provider_url")]
    [TestCase("thumbnail_url")]
    [TestCase("url")]
    public void A_Script_Capable_Url_Is_Removed_From_Every_Url_Field(string field)
    {
        foreach (var unsafeUrl in new[] { "javascript:alert(1)", "data:text/html,x", "file:///etc/passwd", "java\nscript:alert(1)", "\u0001javascript:alert(1)" })
        {
            List<OEmbedValue> result = Convert(Embed($$""" "{{field}}": {{System.Text.Json.JsonSerializer.Serialize(unsafeUrl)}} """));

            Assert.That(result, Has.Count.EqualTo(1), $"{field}: {unsafeUrl}");

            OEmbedResponse response = result[0].OEmbed;
            Uri? value = field switch
            {
                "author_url" => response.AuthorUrl,
                "provider_url" => response.ProviderUrl,
                "thumbnail_url" => response.ThumbnailUrl,
                _ => response.Url,
            };

            Assert.That(value, Is.Null, $"{field}: {unsafeUrl}");
        }
    }

    [Test]
    public void Http_Https_And_Relative_Urls_Are_Kept()
    {
        List<OEmbedValue> result = Convert(Embed("""
            "author_url": "https://example.com/author", "provider_url": "http://example.com/",
            "thumbnail_url": "/thumbs/1.jpg", "url": "https://example.com/photo.jpg"
            """));

        OEmbedResponse response = result[0].OEmbed;
        Assert.That(response.AuthorUrl, Is.EqualTo(new Uri("https://example.com/author")));
        Assert.That(response.ProviderUrl, Is.EqualTo(new Uri("http://example.com/")));
        Assert.That(response.ThumbnailUrl?.OriginalString, Is.EqualTo("/thumbs/1.jpg"));
        Assert.That(response.Url, Is.EqualTo(new Uri("https://example.com/photo.jpg")));
    }

    [Test]
    public void The_Legacy_Single_Object_Shape_Is_Checked_Too()
    {
        List<OEmbedValue> result = Convert("""
            { "url": "https://youtube.com/watch?v=1", "oembed": { "type": "rich", "thumbnail_url": "javascript:alert(1)" } }
            """);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OEmbed.ThumbnailUrl, Is.Null);
    }

    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,x")]
    [TestCase("/relative")]
    [TestCase("")]
    public void An_Item_Whose_Own_Url_Is_Not_Http_Or_Https_Is_Left_Out(string url)
    {
        List<OEmbedValue> result = Convert(Embed("\"title\": \"x\"", url));

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Null_Items_And_Null_Responses_Are_Left_Out_Instead_Of_Reaching_A_View()
    {
        List<OEmbedValue> result = Convert("""
            [null, { "url": "https://youtube.com/watch?v=1", "oembed": null },
             { "url": "https://youtube.com/watch?v=2", "oembed": { "type": "video" } }]
            """);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Url, Is.EqualTo(new Uri("https://youtube.com/watch?v=2")));
    }

    [Test]
    public void The_Html_Is_Left_As_Stored()
    {
        List<OEmbedValue> result = Convert(Embed(""" "html": "<iframe src=\"https://example.com/e\"></iframe>" """));

        Assert.That(result[0].OEmbed.Html, Is.EqualTo("<iframe src=\"https://example.com/e\"></iframe>"));
    }
}
