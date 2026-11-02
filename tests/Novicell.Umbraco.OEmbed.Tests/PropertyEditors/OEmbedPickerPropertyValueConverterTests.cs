using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.Tests.PropertyEditors;

[TestFixture]
public class OEmbedPickerPropertyValueConverterTests
{
    private OEmbedPickerPropertyValueConverter _converter = null!;
    private IPublishedElement _owner = null!;

    [SetUp]
    public void SetUp()
    {
        _converter = new OEmbedPickerPropertyValueConverter();
        _owner = Substitute.For<IPublishedElement>();
    }

    private static IPublishedPropertyType PropertyType(string editorAlias, bool? multiple = null)
    {
        IPublishedPropertyType propertyType = Substitute.For<IPublishedPropertyType>();
        propertyType.EditorAlias.Returns(editorAlias);

        if (multiple is not null)
        {
            var configuration = new OEmbedPickerConfiguration { Multiple = multiple.Value };
            propertyType.DataType.Returns(new PublishedDataType(1, editorAlias, "Novicell.OEmbed.PropertyEditorUi", new Lazy<object?>(() => configuration)));
        }

        return propertyType;
    }

    private static List<OEmbedValue> Embeds(params string[] urls)
        => urls.Select(url => new OEmbedValue
        {
            Url = new Uri(url),
            OEmbed = new OEmbedResponse { Type = OEmbedType.Video },
        }).ToList();

    [Test]
    public void IsConverter_Matches_Only_The_OEmbedPicker_Alias()
    {
        Assert.That(_converter.IsConverter(PropertyType(Constants.PropertyEditors.OEmbedPicker)), Is.True);
        Assert.That(_converter.IsConverter(PropertyType("Umbraco.TextBox")), Is.False);
    }

    [Test]
    public void GetPropertyValueType_Is_A_Collection_When_The_Data_Type_Allows_Multiple()
    {
        Assert.That(_converter.GetPropertyValueType(PropertyType(Constants.PropertyEditors.OEmbedPicker, multiple: true)),
            Is.EqualTo(typeof(IEnumerable<OEmbedValue>)));
    }

    [TestCase(false)]
    [TestCase(null)]
    public void GetPropertyValueType_Is_A_Single_Value_When_The_Data_Type_Does_Not_Allow_Multiple(bool? multiple)
    {
        Assert.That(_converter.GetPropertyValueType(PropertyType(Constants.PropertyEditors.OEmbedPicker, multiple)),
            Is.EqualTo(typeof(OEmbedValue)));
    }

    [Test]
    public void Razor_Value_Is_The_Collection_When_Multiple()
    {
        List<OEmbedValue> value = Embeds("https://a.test/1", "https://a.test/2");

        object? result = _converter.ConvertIntermediateToObject(
            _owner, PropertyType(Constants.PropertyEditors.OEmbedPicker, multiple: true), PropertyCacheLevel.Element, value, false);

        Assert.That(result, Is.SameAs(value));
    }

    [Test]
    public void Razor_Value_Is_An_Empty_Collection_When_Multiple_And_Nothing_Was_Picked()
    {
        object? result = _converter.ConvertIntermediateToObject(
            _owner, PropertyType(Constants.PropertyEditors.OEmbedPicker, multiple: true), PropertyCacheLevel.Element, Embeds(), false);

        Assert.That(result, Is.InstanceOf<IEnumerable<OEmbedValue>>().And.Empty);
    }

    [Test]
    public void Razor_Value_Is_One_Item_When_Not_Multiple()
    {
        List<OEmbedValue> value = Embeds("https://a.test/1");

        object? result = _converter.ConvertIntermediateToObject(
            _owner, PropertyType(Constants.PropertyEditors.OEmbedPicker, multiple: false), PropertyCacheLevel.Element, value, false);

        Assert.That(result, Is.SameAs(value[0]));
    }

    [Test]
    public void Razor_Value_Is_Null_When_Not_Multiple_And_Nothing_Was_Picked()
    {
        object? result = _converter.ConvertIntermediateToObject(
            _owner, PropertyType(Constants.PropertyEditors.OEmbedPicker, multiple: false), PropertyCacheLevel.Element, Embeds(), false);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Razor_Value_Is_The_First_Item_When_A_Data_Type_Was_Changed_To_Single_After_Several_Were_Saved()
    {
        List<OEmbedValue> value = Embeds("https://a.test/1", "https://a.test/2");

        object? result = _converter.ConvertIntermediateToObject(
            _owner, PropertyType(Constants.PropertyEditors.OEmbedPicker, multiple: false), PropertyCacheLevel.Element, value, false);

        Assert.That(result, Is.SameAs(value[0]));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void The_Delivery_Api_Is_An_Array_Whatever_The_Setting_Is(bool multiple)
    {
        var converter = (global::Umbraco.Cms.Core.PropertyEditors.DeliveryApi.IDeliveryApiPropertyValueConverter)_converter;
        IPublishedPropertyType propertyType = PropertyType(Constants.PropertyEditors.OEmbedPicker, multiple);

        Assert.That(converter.GetDeliveryApiPropertyValueType(propertyType), Is.EqualTo(typeof(IEnumerable<Novicell.Umbraco.OEmbed.DeliveryApi.ApiOEmbedValue>)));
        Assert.That(
            converter.ConvertIntermediateToDeliveryApiObject(_owner, propertyType, PropertyCacheLevel.Element, Embeds("https://a.test/1"), false, false),
            Is.InstanceOf<Novicell.Umbraco.OEmbed.DeliveryApi.ApiOEmbedValue[]>().And.Length.EqualTo(1));
    }

    [Test]
    public void ConvertSourceToIntermediate_Parses_Valid_Stored_Json_Array()
    {
        const string json = """
            [
                { "url": "https://youtube.com/watch?v=123", "oembed": { "type": "video", "version": "1.0", "title": "A test video" } },
                { "url": "https://youtube.com/watch?v=456", "oembed": { "type": "video", "version": "1.0", "title": "Another video" } }
            ]
            """;

        var result = (_converter.ConvertSourceToIntermediate(_owner, PropertyType(Constants.PropertyEditors.OEmbedPicker), json, false)
            as IEnumerable<OEmbedValue>)!.ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Url, Is.EqualTo(new Uri("https://youtube.com/watch?v=123")));
        Assert.That(result[0].OEmbed.Type, Is.EqualTo(OEmbedType.Video));
        Assert.That(result[1].OEmbed.Title, Is.EqualTo("Another video"));
    }

    [Test]
    public void ConvertSourceToIntermediate_Wraps_Legacy_Single_Object_Json_In_A_List()
    {
        const string json = """
            {
                "url": "https://youtube.com/watch?v=123",
                "oembed": { "type": "video", "version": "1.0", "title": "A test video" }
            }
            """;

        var result = (_converter.ConvertSourceToIntermediate(_owner, PropertyType(Constants.PropertyEditors.OEmbedPicker), json, false)
            as IEnumerable<OEmbedValue>)!.ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Url, Is.EqualTo(new Uri("https://youtube.com/watch?v=123")));
        Assert.That(result[0].OEmbed.Title, Is.EqualTo("A test video"));
    }

    [Test]
    public void ConvertSourceToIntermediate_Parses_Real_World_Shape_Without_A_Version_Field()
    {
        // A provider can leave the version out, so it is not always in what gets saved. Regression test:
        // the version used to be `required`, which made every such embed fail to deserialize silently.
        const string json = """
            [{ "url": "https://youtube.com/watch?v=123", "oembed": { "type": "video", "width": 200, "height": 113 } }]
            """;

        var result = (_converter.ConvertSourceToIntermediate(_owner, PropertyType(Constants.PropertyEditors.OEmbedPicker), json, false)
            as IEnumerable<OEmbedValue>)!.ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OEmbed.Type, Is.EqualTo(OEmbedType.Video));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("{ not valid json")]
    [TestCase("""{"url": "https://youtube.com/watch?v=123"}""")] // missing required "oembed"
    public void ConvertSourceToIntermediate_Returns_Empty_For_Null_Empty_Or_Malformed_Value(string? source)
    {
        var result = _converter.ConvertSourceToIntermediate(_owner, PropertyType(Constants.PropertyEditors.OEmbedPicker), source, false)
            as IEnumerable<OEmbedValue>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ConvertIntermediateToObject_Returns_The_Parsed_Value_Unchanged()
    {
        var value = new List<OEmbedValue>
        {
            new()
            {
                Url = new Uri("https://youtube.com/watch?v=123"),
                OEmbed = new OEmbedResponse { Type = OEmbedType.Video, Version = "1.0" }
            }
        };

        var result = _converter.ConvertIntermediateToObject(
            _owner,
            PropertyType(Constants.PropertyEditors.OEmbedPicker, multiple: true),
            global::Umbraco.Cms.Core.PropertyEditors.PropertyCacheLevel.Element,
            value,
            false);

        Assert.That(result, Is.SameAs(value));
    }

    [Test]
    public void ConvertIntermediateToObject_Returns_Null_When_Intermediate_Is_Not_An_OEmbedValue_Collection()
    {
        var result = _converter.ConvertIntermediateToObject(
            _owner,
            PropertyType(Constants.PropertyEditors.OEmbedPicker),
            global::Umbraco.Cms.Core.PropertyEditors.PropertyCacheLevel.Element,
            null,
            false);

        Assert.That(result, Is.Null);
    }
}
