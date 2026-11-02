using System.Text.Json;
using System.Text.Json.Nodes;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using Novicell.Umbraco.OEmbed.Tests.TestSupport;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Models.Validation;
using Umbraco.Cms.Core.PropertyEditors.Validation;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace Novicell.Umbraco.OEmbed.Tests.PropertyEditors;

/// <summary>
/// Exercises <see cref="OEmbedPickerPropertyValueEditor.MinMaxValidator"/>/<see cref="OEmbedPickerPropertyValueEditor.AllowedTypeValidator"/> the way
/// <see cref="OEmbedPickerPropertyValueEditor"/> actually wires them up - through the real
/// <see cref="TypedJsonValidatorRunner{TValue,TConfiguration}"/> and a real <see cref="IJsonSerializer"/>,
/// mirroring <c>MediaPicker3PropertyValueEditor</c>'s own constructor exactly, with no extra normalization.
/// </summary>
[TestFixture]
public class OEmbedValidationIntegrationTests
{
    private IJsonSerializer _jsonSerializer = null!;
    private ILocalizedTextService _localizedTextService = null!;

    [SetUp]
    public void SetUp()
    {
        _jsonSerializer = TestJsonSerializer.Create();
        _localizedTextService = Substitute.For<ILocalizedTextService>();
        _localizedTextService
            .Localize(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<System.Globalization.CultureInfo?>(), Arg.Any<IDictionary<string, string?>?>())
            .Returns("localized message");
    }

    private TypedJsonValidatorRunner<List<OEmbedDto>, OEmbedPickerConfiguration> BuildRunner()
        => new(
            _jsonSerializer,
            new OEmbedPickerPropertyValueEditor.MinMaxValidator(_localizedTextService),
            new OEmbedPickerPropertyValueEditor.AllowedTypeValidator(_localizedTextService),
            new OEmbedPickerPropertyValueEditor.UrlValidator(_localizedTextService));

    private static PropertyValidationContext Context() => PropertyValidationContext.Empty();

    private const string OneEmbed = """
        [{ "url": "https://youtube.com/watch?v=1", "oembed": { "type": "video", "version": "1.0" } }]
        """;

    [Test]
    public void Array_Shaped_JsonNode_Value_Is_Validated()
    {
        // This is the shape the Management API's JsonObjectConverter actually produces for a posted
        // array value on a real publish request (a JsonArray, not a bare JsonElement) - confirmed by
        // tracing ConfigureUmbracoBackofficeJsonOptions/JsonObjectConverter.
        JsonNode? value = JsonNode.Parse(OneEmbed);

        var results = BuildRunner().Validate(value, null, new OEmbedPickerConfiguration
        {
            Multiple = true,
            ValidationLimit = new OEmbedPickerConfiguration.NumberRange { Min = 2 },
        }, Context());

        Assert.That(results, Is.Not.Empty);
    }

    [Test]
    public void Array_Shaped_Json_String_Value_Is_Validated()
    {
        // The published-cache/storage shape (a raw JSON string) is also handled directly by
        // IJsonSerializer.TryDeserialize, with no normalization needed.
        var results = BuildRunner().Validate(OneEmbed, null, new OEmbedPickerConfiguration
        {
            Multiple = true,
            ValidationLimit = new OEmbedPickerConfiguration.NumberRange { Min = 2 },
        }, Context());

        Assert.That(results, Is.Not.Empty);
    }

    [Test]
    public void Legacy_Single_Object_Json_Throws_Like_Media_Picker_3_Would_For_An_Equivalent_Shape()
    {
        // Known, accepted limitation: Umbraco's own TypedJsonValidatorRunner/IJsonSerializer.TryDeserialize
        // has no tolerance for a bare JSON object where a JSON array is expected - the same class Media
        // Picker 3 relies on, unwrapped (MediaPicker3PropertyValueEditor's constructor adds it directly to
        // Validators with no normalization either). An earlier, since removed, helper of this project
        // helper used to paper over this for legacy single-object content saved before multi-value support,
        // but we've deliberately chosen to match Media Picker 3's own risk profile here instead of keeping a
        // bespoke wrapper around Umbraco's shared validation infrastructure.
        const string legacySingleObject = """
            { "url": "https://youtube.com/watch?v=1", "oembed": { "type": "video", "version": "1.0" } }
            """;

        Assert.Throws<JsonException>(() => BuildRunner().Validate(legacySingleObject, null, new OEmbedPickerConfiguration
        {
            Multiple = true,
            ValidationLimit = new OEmbedPickerConfiguration.NumberRange { Min = 1 },
        }, Context()));
    }

    [Test]
    public void Allowed_Type_Check_Runs_Through_The_Real_Runner()
    {
        const string disallowed = """
            [{ "url": "https://flickr.com/photo/1", "oembed": { "type": "photo", "version": "1.0" } }]
            """;

        var results = BuildRunner().Validate(disallowed, null, new OEmbedPickerConfiguration
        {
            Multiple = true,
            AllowedTypes = ["video"],
        }, Context());

        Assert.That(results, Is.Not.Empty);
    }

    [Test]
    public void A_Script_Url_In_The_Posted_Json_Is_Rejected_Through_The_Real_Runner()
    {
        const string withScriptUrl = """
            [{ "url": "https://youtube.com/watch?v=1", "oembed": { "type": "rich", "version": "1.0", "author_url": "javascript:alert(1)", "thumbnail_url": "https://example.com/t.jpg" } }]
            """;

        var results = BuildRunner().Validate(withScriptUrl, null, new OEmbedPickerConfiguration { Multiple = true }, Context());

        Assert.That(results, Is.Not.Empty);
    }

    [TestCase("""[{ "url": "https://youtube.com/watch?v=1", "oembed": { "type": "rich", "author_url": "java\nscript:alert(1)" } }]""")]
    [TestCase("""[{ "url": "https://youtube.com/watch?v=1", "oembed": { "type": "rich", "thumbnail_url": "\u0001javascript:alert(1)" } }]""")]
    [TestCase("""[{ "url": "https://youtube.com/watch?v=1", "oembed": { "type": "rich", "provider_url": "data:text/html,x" } }]""")]
    public void A_Control_Character_Url_In_The_Posted_Json_Is_Rejected_Through_The_Real_Runner(string json)
    {
        var results = BuildRunner().Validate(json, null, new OEmbedPickerConfiguration { Multiple = true }, Context());

        Assert.That(results, Is.Not.Empty);
    }

    [TestCase("[null]")]
    [TestCase("""[{ "url": "https://youtube.com/watch?v=1", "oembed": null }]""")]
    [TestCase("""[{ "url": null, "oembed": { "type": "video" } }]""")]
    [TestCase("""[{ "url": "https://youtube.com/watch?v=1", "oembed": { "type": "video" } }, null]""")]
    public void Explicit_Nulls_In_The_Posted_Json_Are_Rejected_Without_Throwing(string json)
    {
        // With and without an allowed-type restriction: both validators must cope with the shape.
        foreach (string[] allowedTypes in new[] { Array.Empty<string>(), new[] { "video" } })
        {
            var results = BuildRunner().Validate(json, null, new OEmbedPickerConfiguration
            {
                Multiple = true,
                AllowedTypes = allowedTypes,
            }, Context()).ToList();

            Assert.That(results, Is.Not.Empty, $"allowed types: {string.Join(",", allowedTypes)}");
        }
    }

    [Test]
    public void Ordinary_Http_Urls_In_The_Posted_Json_Pass_Through_The_Real_Runner()
    {
        const string ordinary = """
            [{ "url": "https://youtube.com/watch?v=1", "oembed": { "type": "video", "version": "1.0", "author_url": "https://youtube.com/@a", "provider_url": "https://youtube.com/", "thumbnail_url": "https://i.ytimg.com/vi/1/hq.jpg" } }]
            """;

        var results = BuildRunner().Validate(ordinary, null, new OEmbedPickerConfiguration { Multiple = true }, Context());

        Assert.That(results, Is.Empty);
    }
}
