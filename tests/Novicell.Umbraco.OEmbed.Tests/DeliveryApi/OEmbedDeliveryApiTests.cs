using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Novicell.Umbraco.OEmbed.DeliveryApi;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using Novicell.Umbraco.OEmbed.Tests.TestSupport;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.DeliveryApi;

namespace Novicell.Umbraco.OEmbed.Tests.DeliveryApi;

/// <summary>
/// The JSON a headless site receives for an OEmbed picker property. These tests pin it: it is a contract
/// that a site depends on, so it must not change as a side effect of changing the model used in Razor.
/// </summary>
[TestFixture]
public class OEmbedDeliveryApiTests
{
    private readonly OEmbedPickerPropertyValueConverter _converter = new();
    private readonly IPublishedElement _owner = Substitute.For<IPublishedElement>();
    private readonly IPublishedPropertyType _propertyType = Substitute.For<IPublishedPropertyType>();

    [SetUp]
    public void SetUp() => _propertyType.EditorAlias.Returns(Constants.PropertyEditors.OEmbedPicker);

    // Close to what Umbraco's Delivery API writes: web defaults (camelCase). The encoder only keeps
    // the expected JSON in these tests readable.
    private static JsonSerializerOptions Web => new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private object? DeliveryApiValue(string? stored, bool expanding = false)
    {
        object? intermediate = _converter.ConvertSourceToIntermediate(_owner, _propertyType, stored, false);

        return ((IDeliveryApiPropertyValueConverter)_converter).ConvertIntermediateToDeliveryApiObject(
            _owner, _propertyType, PropertyCacheLevel.Element, intermediate, false, expanding);
    }

    private string Json(string? stored, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(DeliveryApiValue(stored), options ?? Web);

    private static void AssertSameJson(string expected, string actual)
        => Assert.That(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)), Is.True, actual);

    [Test]
    public void The_Converter_Takes_Part_In_The_Delivery_Api()
        => Assert.That(_converter, Is.InstanceOf<IDeliveryApiPropertyValueConverter>());

    [Test]
    public void The_Delivery_Api_Value_Is_A_List_Of_The_Dto()
    {
        var converter = (IDeliveryApiPropertyValueConverter)_converter;

        Assert.That(converter.GetDeliveryApiPropertyValueType(_propertyType), Is.EqualTo(typeof(IEnumerable<ApiOEmbedValue>)));
        Assert.That(converter.GetDeliveryApiPropertyCacheLevel(_propertyType), Is.EqualTo(PropertyCacheLevel.Element));
        Assert.That(DeliveryApiValue(StoredValues.Current), Is.InstanceOf<IEnumerable<ApiOEmbedValue>>());
    }

    [Test]
    public void A_Value_Saved_By_This_Version_Is_Returned_In_Camel_Case_With_Nulls_For_What_Was_Not_Returned()
    {
        AssertSameJson(
            """
            [
              {
                "url": "https://www.youtube.com/watch?v=XbWCjWBuTDU",
                "oembed": {
                  "type": "video",
                  "version": "1.0",
                  "title": "BMW 840i Harman Kardon Upgrade",
                  "authorName": "Cambridge Car Audio",
                  "authorUrl": "https://www.youtube.com/@CambridgeCarAudio",
                  "providerName": "YouTube",
                  "providerUrl": "https://www.youtube.com/",
                  "thumbnailUrl": "https://i.ytimg.com/vi/XbWCjWBuTDU/hqdefault.jpg",
                  "thumbnailHeight": 360,
                  "thumbnailWidth": 480,
                  "html": "<iframe width=\"200\" height=\"113\" src=\"https://www.youtube.com/embed/XbWCjWBuTDU?feature=oembed\" frameborder=\"0\" allowfullscreen></iframe>",
                  "url": null,
                  "height": 113,
                  "width": 200,
                  "cacheAge": null
                }
              }
            ]
            """,
            Json(StoredValues.Current));
    }


    [Test]
    public void A_Value_Saved_By_The_Umbraco_9_Package_Is_Returned_With_The_Same_Names_As_Any_Other()
    {
        // One object in, an array out, and camelCase names like every other value.
        AssertSameJson(
            """
            [
              {
                "url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                "oembed": {
                  "type": "video",
                  "version": "1.0",
                  "title": "Rick Astley - Never Gonna Give You Up (Official Music Video)",
                  "authorName": "Rick Astley",
                  "authorUrl": "https://www.youtube.com/c/RickastleyCoUkOfficial",
                  "providerName": "YouTube",
                  "providerUrl": "https://www.youtube.com/",
                  "thumbnailUrl": "https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg",
                  "thumbnailHeight": 360,
                  "thumbnailWidth": 480,
                  "html": "<iframe width=\"200\" height=\"113\" src=\"https://www.youtube.com/embed/dQw4w9WgXcQ?feature=oembed\" frameborder=\"0\" allowfullscreen></iframe>",
                  "url": null,
                  "height": 113,
                  "width": 200,
                  "cacheAge": 3600
                }
              }
            ]
            """,
            Json(StoredValues.Umbraco9));
    }

    [Test]
    public void The_Names_Do_Not_Depend_On_The_Serializer_Settings()
    {
        // With no naming policy a property would be written as it is named in C# (Url, OEmbed, ...).
        var options = new JsonSerializerOptions { PropertyNamingPolicy = null, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        AssertSameJson(Json(StoredValues.Current), Json(StoredValues.Current, options));
    }

    [Test]
    public void The_Properties_Are_Written_In_A_Fixed_Order()
    {
        JsonObject item = JsonNode.Parse(Json(StoredValues.Current))!.AsArray()[0]!.AsObject();

        Assert.That(item.Select(p => p.Key), Is.EqualTo(new[] { "url", "oembed" }));
        Assert.That(item["oembed"]!.AsObject().Select(p => p.Key), Is.EqualTo(new[]
        {
            "type", "version", "title", "authorName", "authorUrl", "providerName", "providerUrl",
            "thumbnailUrl", "thumbnailHeight", "thumbnailWidth", "html", "url", "height", "width", "cacheAge",
        }));
    }

    [TestCase("photo")]
    [TestCase("rich")]
    [TestCase("link")]
    public void The_Type_Is_The_Lowercase_Value_Of_The_oEmbed_Specification(string type)
    {
        var stored = $$"""[{ "url": "https://example.com/x", "oembed": { "type": "{{type}}" } }]""";

        JsonNode oembed = JsonNode.Parse(Json(stored))!.AsArray()[0]!["oembed"]!;

        Assert.That((string?)oembed["type"], Is.EqualTo(type));
    }

    private const string StoredWithExtras = """
        [{ "url": "https://vimeo.com/1",
           "oembed": { "type": "video", "title": "A", "video_id": 1, "is_plus": "0",
                       "upload_date": "2019-10-02 10:10:10", "nested": { "a_b": [1, 2] } } }]
        """;

    [Test]
    public void What_The_Provider_Returned_Without_A_Property_Is_Returned_In_Extra_With_The_Providers_Names()
    {
        JsonNode oembed = JsonNode.Parse(Json(StoredWithExtras))!.AsArray()[0]!["oembed"]!;

        Assert.That(oembed["extra"]!.AsObject().Select(field => field.Key),
            Is.EquivalentTo(new[] { "video_id", "is_plus", "upload_date", "nested" }));
        Assert.That((int?)oembed["extra"]!["video_id"], Is.EqualTo(1), "a number stays a number");
        Assert.That((string?)oembed["extra"]!["is_plus"], Is.EqualTo("0"), "text stays text");
        Assert.That((int?)oembed["extra"]!["nested"]!["a_b"]![1], Is.EqualTo(2), "nothing is renamed or flattened");
        Assert.That(oembed.AsObject().ContainsKey("video_id"), Is.False, "the typed fields are not mixed with the extras");
    }

    [Test]
    public void Extra_Is_Left_Out_When_The_Provider_Returned_Nothing_More()
    {
        JsonNode oembed = JsonNode.Parse(Json(StoredValues.Current))!.AsArray()[0]!["oembed"]!;

        Assert.That(oembed.AsObject().ContainsKey("extra"), Is.False);
    }

    [Test]
    public void The_Names_In_Extra_Do_Not_Depend_On_The_Serializer_Settings()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        JsonNode oembed = JsonNode.Parse(Json(StoredWithExtras, options))!.AsArray()[0]!["oembed"]!;

        Assert.That(oembed["extra"]!.AsObject().ContainsKey("video_id"), Is.True);
        Assert.That(oembed["extra"]!.AsObject().ContainsKey("videoId"), Is.False);
    }

    [Test]
    public void A_Script_Capable_Url_Never_Reaches_The_Api()
    {
        var stored = """
            [{ "url": "https://example.com/x",
               "oembed": { "type": "rich", "author_url": "javascript:alert(1)", "thumbnail_url": "data:text/html,x", "provider_url": "java\nscript:alert(1)" } }]
            """;

        JsonNode oembed = JsonNode.Parse(Json(stored))!.AsArray()[0]!["oembed"]!;

        Assert.That(oembed["authorUrl"], Is.Null);
        Assert.That(oembed["thumbnailUrl"], Is.Null);
        Assert.That(oembed["providerUrl"], Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not json")]
    [TestCase("[]")]
    [TestCase("""[null, { "url": "javascript:alert(1)", "oembed": { "type": "rich" } }]""")]
    public void Nothing_Usable_Is_An_Empty_Array_Not_Null(string? stored)
        => Assert.That(Json(stored), Is.EqualTo("[]"));

    [Test]
    public void Expanding_Makes_No_Difference()
        => Assert.That(
            JsonSerializer.Serialize(DeliveryApiValue(StoredValues.Current, expanding: true), Web),
            Is.EqualTo(Json(StoredValues.Current)));

    [Test]
    public void The_Value_Used_In_Razor_Is_Unchanged()
    {
        var configuration = new OEmbedPickerConfiguration { Multiple = true };
        _propertyType.DataType.Returns(new PublishedDataType(1, Constants.PropertyEditors.OEmbedPicker, "Novicell.OEmbed.PropertyEditorUi", new Lazy<object?>(() => configuration)));

        object? razor = _converter.ConvertIntermediateToObject(
            _owner,
            _propertyType,
            PropertyCacheLevel.Element,
            _converter.ConvertSourceToIntermediate(_owner, _propertyType, StoredValues.Current, false),
            false);

        Assert.That(razor, Is.InstanceOf<IEnumerable<OEmbedValue>>());
    }
}
