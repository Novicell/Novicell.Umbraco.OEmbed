using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using Novicell.Umbraco.OEmbed.Tests.TestSupport;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Novicell.Umbraco.OEmbed.Tests.PropertyEditors;

/// <summary>
/// Content saved in an older shape. The Umbraco 9 version of the package stored one object, where this
/// version stores an array, and pre-release builds of this version stored camelCase names (authorName,
/// thumbnailUrl, ...) where the oEmbed specification, and now this version, use snake_case (author_name,
/// thumbnail_url, ...). A site that still has such content must get the whole value back, not only the
/// fields whose names happen to be the same.
/// </summary>
[TestFixture]
public class OEmbedLegacyValueTests
{
    private const string StoredByTheUmbraco9Package = StoredValues.Umbraco9;

    private static List<OEmbedValue> Convert(string json)
    {
        IPublishedPropertyType propertyType = Substitute.For<IPublishedPropertyType>();
        propertyType.EditorAlias.Returns(Constants.PropertyEditors.OEmbedPicker);

        return ((IEnumerable<OEmbedValue>)new OEmbedPickerPropertyValueConverter()
            .ConvertSourceToIntermediate(Substitute.For<IPublishedElement>(), propertyType, json, false)!).ToList();
    }

    [Test]
    public void A_Value_Stored_By_The_Umbraco_9_Package_Keeps_Every_Field()
    {
        List<OEmbedValue> result = Convert(StoredByTheUmbraco9Package);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Url, Is.EqualTo(new Uri("https://www.youtube.com/watch?v=dQw4w9WgXcQ")));

        OEmbedResponse response = result[0].OEmbed;
        Assert.That(response.Type, Is.EqualTo(OEmbedType.Video));
        Assert.That(response.Version, Is.EqualTo("1.0"));
        Assert.That(response.Title, Is.EqualTo("Rick Astley - Never Gonna Give You Up (Official Music Video)"));
        Assert.That(response.AuthorName, Is.EqualTo("Rick Astley"));
        Assert.That(response.AuthorUrl, Is.EqualTo(new Uri("https://www.youtube.com/c/RickastleyCoUkOfficial")));
        Assert.That(response.ProviderName, Is.EqualTo("YouTube"));
        Assert.That(response.ProviderUrl, Is.EqualTo(new Uri("https://www.youtube.com/")));
        Assert.That(response.ThumbnailUrl, Is.EqualTo(new Uri("https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg")));
        Assert.That(response.ThumbnailHeight, Is.EqualTo(360));
        Assert.That(response.ThumbnailWidth, Is.EqualTo(480));
        Assert.That(response.Width, Is.EqualTo(200));
        Assert.That(response.Height, Is.EqualTo(113));
        Assert.That(response.CacheAge, Is.EqualTo(3600));
        Assert.That(response.Html, Does.StartWith("<iframe"));
    }




    [Test]
    public void A_Value_In_The_Old_Shape_Does_Not_Skip_The_Url_Safety_Check()
    {
        List<OEmbedValue> result = Convert("""
            { "url": "https://youtube.com/watch?v=1",
              "oembed": { "type": "video", "author_url": "javascript:alert(1)", "thumbnail_url": "data:text/html,x" } }
            """);

        Assert.That(result[0].OEmbed.AuthorUrl, Is.Null);
        Assert.That(result[0].OEmbed.ThumbnailUrl, Is.Null);
    }

    [Test]
    public void Duplicate_Property_Names_Do_Not_Make_The_Converter_Throw()
    {
        Assert.DoesNotThrow(() => Convert("""
            { "url": "https://youtube.com/watch?v=1", "oembed": { "type": "video", "title": "a", "title": "b" } }
            """));
    }
}
