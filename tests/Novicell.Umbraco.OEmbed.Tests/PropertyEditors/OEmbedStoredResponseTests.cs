using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Novicell.Umbraco.OEmbed.Api.Controllers;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using Novicell.Umbraco.OEmbed.Services;
using Novicell.Umbraco.OEmbed.Tests.TestSupport;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Models.Validation;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using IOEmbedService = Novicell.Umbraco.OEmbed.Services.IOEmbedService;

namespace Novicell.Umbraco.OEmbed.Tests.PropertyEditors;

/// <summary>
/// What is stored is the response as the provider returned it, with the provider's names and everything
/// else it sent, and <see cref="OEmbedPickerPropertyValueConverter"/> reads it into an
/// <see cref="OEmbedValue"/> leniently.
/// </summary>
[TestFixture]
public class OEmbedStoredResponseTests
{
    private const string WithExtras = """
        [{ "url": "https://www.youtube.com/watch?v=1",
           "oembed": { "type": "video", "version": "1.0", "title": "A title", "html": "<iframe></iframe>",
                       "author_name": "An author", "author_url": "https://www.youtube.com/@a",
                       "provider_name": "YouTube", "provider_url": "https://www.youtube.com/",
                       "thumbnail_url": "https://i.ytimg.com/vi/1/hq.jpg", "thumbnail_width": 480,
                       "thumbnail_height": 360, "cache_age": 3600, "width": 200, "height": 113,
                       "thumbnail_url_with_play_button": "https://i.ytimg.com/vi/1/play.jpg",
                       "is_plus": 1, "tags": ["a", "b"], "nothing": null } }]
        """;

    private static OEmbedResponse Read(string stored)
    {
        IPublishedPropertyType propertyType = Substitute.For<IPublishedPropertyType>();
        propertyType.EditorAlias.Returns(Constants.PropertyEditors.OEmbedPicker);

        return ((IEnumerable<OEmbedValue>)new OEmbedPickerPropertyValueConverter()
            .ConvertSourceToIntermediate(Substitute.For<IPublishedElement>(), propertyType, stored, false)!)
            .Single().OEmbed;
    }

    private static List<OEmbedValue> ReadAll(string stored)
    {
        IPublishedPropertyType propertyType = Substitute.For<IPublishedPropertyType>();
        propertyType.EditorAlias.Returns(Constants.PropertyEditors.OEmbedPicker);

        return ((IEnumerable<OEmbedValue>)new OEmbedPickerPropertyValueConverter()
            .ConvertSourceToIntermediate(Substitute.For<IPublishedElement>(), propertyType, stored, false)!).ToList();
    }

    private static OEmbedPickerPropertyValueEditor Editor()
    {
        ILocalizedTextService text = Substitute.For<ILocalizedTextService>();
        text.Localize(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<System.Globalization.CultureInfo?>(), Arg.Any<IDictionary<string, string?>?>())
            .Returns("localized message");

        return new OEmbedPickerPropertyValueEditor(
            Substitute.For<IShortStringHelper>(),
            TestJsonSerializer.Create(),
            Substitute.For<IIOHelper>(),
            new DataEditorAttribute(Constants.PropertyEditors.OEmbedPicker) { ValueType = ValueTypes.Json },
            text);
    }

    // ---- What the API returns, and what is stored ----

    // What Umbraco's Management API writes: web defaults (camelCase), and enums as text.
    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static OEmbedResponse ProviderResponse() => JsonSerializer.Deserialize<OEmbedResponse>("""
        { "type": "video", "version": 1.0, "title": "t", "author_name": "a", "width": "560",
          "thumbnail_url_with_play_button": "https://i.test/play.jpg", "is_plus": 1, "tags": ["a", "b"] }
        """)!;

    [Test]
    public async Task The_Api_Returns_The_Response_With_Everything_The_Provider_Returned()
    {
        IOEmbedService service = Substitute.For<IOEmbedService>();
        service.GetOEmbedAsync(Arg.Any<OEmbedRequest>(), Arg.Any<CancellationToken>())
            .Returns(Attempt<OEmbedResponse?, OEmbedOperationStatus>.Succeed(OEmbedOperationStatus.Success, ProviderResponse()));

        IActionResult result = await new OEmbedApiController(service).GetOEmbedAsync(new Uri("https://youtube.com/watch?v=1"), null, null, CancellationToken.None);

        var model = (OEmbedResponse)((OkObjectResult)result).Value!;
        Assert.That(model.Extra!.Keys, Is.EquivalentTo(new[] { "thumbnail_url_with_play_button", "is_plus", "tags" }));
    }

    [Test]
    public void The_Api_Response_Is_Written_With_The_Providers_Names_And_Every_Field_It_Had()
    {
        // The Management API writes camelCase by default; a name that is written out is not changed by that,
        // and neither are the names of the extra fields.
        var json = JsonNode.Parse(JsonSerializer.Serialize(
            ProviderResponse(),
            ApiJson))!.AsObject();

        Assert.That(json.Select(p => p.Key), Is.EquivalentTo(new[]
        {
            "type", "version", "title", "author_name", "width",
            "thumbnail_url_with_play_button", "is_plus", "tags",
        }));
        Assert.That((string?)json["type"], Is.EqualTo("video"));
        Assert.That((string?)json["version"], Is.EqualTo("1.0"));
        Assert.That((int?)json["width"], Is.EqualTo(560));
        Assert.That((string?)json["thumbnail_url_with_play_button"], Is.EqualTo("https://i.test/play.jpg"));
    }

    [Test]
    public void A_Field_The_Provider_Did_Not_Return_Is_Left_Out_Of_The_Api_Response()
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(
            new OEmbedResponse { Type = OEmbedType.Link },
            ApiJson))!.AsObject();

        Assert.That(json.Select(p => p.Key), Is.EqualTo(new[] { "type" }));
    }

    [Test]
    public void What_The_Api_Returns_Is_Read_Back_As_The_Stored_Value_With_Every_Field()
    {
        // The client stores what the API returned, with the url the editor entered.
        var response = JsonSerializer.Serialize(ProviderResponse(), ApiJson);
        OEmbedResponse value = Read($$"""[{ "url": "https://youtube.com/watch?v=1", "oembed": {{response}} }]""");

        Assert.That(value.AuthorName, Is.EqualTo("a"));
        Assert.That(value.Width, Is.EqualTo(560));
        Assert.That(value.TryGetValue("is_plus", out int plus), Is.True);
        Assert.That(plus, Is.EqualTo(1));
        Assert.That(value.TryGetValue("tags", out string[]? tags), Is.True);
        Assert.That(tags, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void A_Value_Is_Stored_As_Posted_With_Every_Field_The_Provider_Returned()
    {
        // The editor's value is a JsonArray; nothing is renamed or dropped on the way in.
        var posted = JsonNode.Parse(WithExtras)!;

        object? saved = Editor().FromEditor(new ContentPropertyData(posted, null), null);

        var stored = saved as string ?? JsonSerializer.Serialize(saved);
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(stored), JsonNode.Parse(WithExtras)), Is.True, stored);
    }

    // ---- Reading the stored value into an OEmbedValue ----

    [Test]
    public void The_Known_Fields_Are_Mapped_To_Typed_Properties()
    {
        OEmbedResponse value = Read(WithExtras);

        Assert.That(value.Type, Is.EqualTo(OEmbedType.Video));
        Assert.That(value.Version, Is.EqualTo("1.0"));
        Assert.That(value.AuthorName, Is.EqualTo("An author"));
        Assert.That(value.AuthorUrl, Is.EqualTo(new Uri("https://www.youtube.com/@a")));
        Assert.That(value.ProviderName, Is.EqualTo("YouTube"));
        Assert.That(value.ThumbnailUrl, Is.EqualTo(new Uri("https://i.ytimg.com/vi/1/hq.jpg")));
        Assert.That(value.ThumbnailWidth, Is.EqualTo(480));
        Assert.That(value.CacheAge, Is.EqualTo(3600));
        Assert.That(value.Width, Is.EqualTo(200));
    }

    [Test]
    public void Everything_Without_A_Property_Is_Kept_And_Can_Be_Read()
    {
        OEmbedResponse value = Read(WithExtras);

        Assert.That(value.Extra!.Keys, Is.EquivalentTo(new[] { "thumbnail_url_with_play_button", "is_plus", "tags", "nothing" }));
        Assert.That(value.TryGetValue("thumbnail_url_with_play_button", out string? url), Is.True);
        Assert.That(url, Is.EqualTo("https://i.ytimg.com/vi/1/play.jpg"));
        Assert.That(value.TryGetValue("is_plus", out int plus), Is.True);
        Assert.That(plus, Is.EqualTo(1));
        Assert.That(value.TryGetValue("tags", out string[]? tags), Is.True);
        Assert.That(tags, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void A_Field_That_Is_Not_There_Or_Is_Another_Kind_Is_Not_Read()
    {
        OEmbedResponse value = Read(WithExtras);

        Assert.That(value.TryGetValue("missing", out string? missing), Is.False);
        Assert.That(missing, Is.Null);
        Assert.That(value.TryGetValue("tags", out int wrongKind), Is.False);
        Assert.That(wrongKind, Is.EqualTo(0));
        Assert.That(value.TryGetValue("nothing", out string? nothing), Is.False);
        Assert.That(nothing, Is.Null);
        Assert.That(Read("""[{ "url": "https://a.test/1", "oembed": { "type": "video" } }]""").Extra, Is.Null);
    }

    [TestCase("\"560\"", 560)]
    [TestCase("560", 560)]
    [TestCase("560.0", 560)]
    [TestCase(" \"560\" ", 560)]
    [TestCase("\"abc\"", null)]
    [TestCase("560.5", null)]
    [TestCase("99999999999", null)]
    [TestCase("true", null)]
    [TestCase("null", null)]
    [TestCase("[560]", null)]
    public void A_Number_Is_Read_Leniently_And_Anything_Else_Is_Null(string json, int? expected)
    {
        OEmbedResponse value = Read($$"""[{ "url": "https://a.test/1", "oembed": { "type": "video", "width": {{json}} } }]""");

        Assert.That(value.Width, Is.EqualTo(expected));
    }

    [Test]
    public void One_Field_Of_The_Wrong_Kind_Does_Not_Make_The_Value_Unreadable()
    {
        OEmbedResponse value = Read("""
            [{ "url": "https://a.test/1",
               "oembed": { "type": "video", "title": 5, "author_url": ["x"], "width": "wide", "height": 113, "provider_name": "YouTube" } }]
            """);

        Assert.That(value.Title, Is.Null);
        Assert.That(value.AuthorUrl, Is.Null);
        Assert.That(value.Width, Is.Null);
        Assert.That(value.Height, Is.EqualTo(113));
        Assert.That(value.ProviderName, Is.EqualTo("YouTube"));
    }

    [TestCase("Video", OEmbedType.Video)]
    [TestCase("PHOTO", OEmbedType.Photo)]
    [TestCase("rich", OEmbedType.Rich)]
    [TestCase("link", OEmbedType.Link)]
    public void The_Type_Is_Read_Whatever_The_Casing(string type, OEmbedType expected)
        => Assert.That(Read($$"""[{ "url": "https://a.test/1", "oembed": { "type": "{{type}}" } }]""").Type, Is.EqualTo(expected));

    [TestCase("""{ "title": "no type" }""")]
    [TestCase("""{ "type": "movie" }""")]
    [TestCase("""{ "type": "1" }""")]
    // Enum parsing alone would accept all of these: numbers with a sign or a space, a combination, and padding.
    [TestCase("""{ "type": " 1" }""")]
    [TestCase("""{ "type": "-1" }""")]
    [TestCase("""{ "type": "+1" }""")]
    [TestCase("""{ "type": "video,photo" }""")]
    [TestCase("""{ "type": "video, photo" }""")]
    [TestCase("""{ "type": " video" }""")]
    [TestCase("""{ "type": "video " }""")]
    [TestCase("""{ "type": 1 }""")]
    [TestCase("""{ "type": null }""")]
    [TestCase("""{ "type": "" }""")]
    public void An_Item_Without_A_Valid_Type_Is_Left_Out_And_The_Others_Are_Kept(string oembed)
    {
        List<OEmbedValue> result = ReadAll($$"""
            [{ "url": "https://a.test/1", "oembed": {{oembed}} },
             { "url": "https://a.test/2", "oembed": { "type": "video" } }]
            """);

        Assert.That(result.Select(r => r.Url.OriginalString), Is.EqualTo(new[] { "https://a.test/2" }));
    }

    [TestCase("""{ "url": 5, "oembed": { "type": "video" } }""")]
    [TestCase("""{ "url": "https://a.test/1", "oembed": "text" }""")]
    [TestCase("""{ "url": "https://a.test/1", "oembed": [] }""")]
    [TestCase("""5""")]
    [TestCase("""null""")]
    public void An_Item_Of_The_Wrong_Shape_Is_Left_Out_And_The_Others_Are_Kept(string item)
    {
        List<OEmbedValue> result = ReadAll($$"""
            [{{item}}, { "url": "https://a.test/2", "oembed": { "type": "video" } }]
            """);

        Assert.That(result.Select(r => r.Url.OriginalString), Is.EqualTo(new[] { "https://a.test/2" }));
    }

    [Test]
    public void A_Property_Name_That_Is_There_Twice_Is_Read_With_The_Last_Value_And_Does_Not_Fail_The_Value()
    {
        List<OEmbedValue> result = ReadAll("""
            [{ "url": "https://a.test/1", "oembed": { "type": "video", "title": "a", "title": "b", "x": 1, "x": 2 } },
             { "url": "https://a.test/2", "oembed": { "type": "video" } }]
            """);

        Assert.That(result.Select(r => r.Url.OriginalString), Is.EqualTo(new[] { "https://a.test/1", "https://a.test/2" }));
        Assert.That(result[0].OEmbed.Title, Is.EqualTo("b"));
        Assert.That(result[0].OEmbed.TryGetValue("x", out int x), Is.True);
        Assert.That(x, Is.EqualTo(2));
    }

    [Test]
    public void The_Dto_Reads_An_Array_And_The_Single_Object_Shape()
    {
        const string item = """{ "url": "https://a.test/1", "oembed": { "type": "video", "x": 1 } }""";

        List<OEmbedDto?> fromArray = OEmbedDto.Deserialize($"[{item},{item}]");
        List<OEmbedDto?> fromObject = OEmbedDto.Deserialize(item);

        Assert.That(fromArray, Has.Count.EqualTo(2));
        Assert.That(fromObject, Has.Count.EqualTo(1));
        Assert.That(fromObject[0]!.Url, Is.EqualTo("https://a.test/1"));
        Assert.That(fromObject[0]!.OEmbed!.Extra!["x"].GetInt32(), Is.EqualTo(1), "what has no property is kept");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not json")]
    [TestCase("5")]
    [TestCase("\"text\"")]
    public void The_Dto_Gives_Nothing_For_A_Value_That_Is_Not_An_Array_Or_An_Object(string? json)
        => Assert.That(OEmbedDto.Deserialize(json), Is.Empty);

    [Test]
    public void The_Names_Are_The_Providers_Only()
    {
        // A name in another spelling is not a field of the response: it is an extra, never a URL we
        // would hand on as a known one.
        OEmbedResponse value = Read("""[{ "url": "https://a.test/1", "oembed": { "type": "video", "authorUrl": "javascript:alert(1)", "AUTHOR_URL": "javascript:alert(1)" } }]""");

        Assert.That(value.AuthorUrl, Is.Null);
        Assert.That(value.Extra!.Keys, Is.EquivalentTo(new[] { "authorUrl", "AUTHOR_URL" }));
    }

    // ---- Validation of what the backoffice posts ----

    [TestCase("author_url", "javascript:alert(1)")]
    [TestCase("provider_url", "data:text/html,x")]
    [TestCase("thumbnail_url", "\u0001javascript:alert(1)")]
    [TestCase("url", "java\nscript:alert(1)")]
    public void What_The_Backoffice_Posts_Is_Validated_With_The_Providers_Names(string field, string unsafeUrl)
    {
        JsonNode posted = JsonNode.Parse(WithExtras)!;
        posted.AsArray()[0]!["oembed"]![field] = unsafeUrl;

        var results = Editor().Validators
            .SelectMany(v => v.Validate(posted, ValueTypes.Json, new OEmbedPickerConfiguration { Multiple = true }, PropertyValidationContext.Empty()))
            .ToList();

        Assert.That(results, Is.Not.Empty, field);
    }

    [Test]
    public void A_Value_With_Extra_Fields_Posted_By_The_Backoffice_Is_Valid()
    {
        var results = Editor().Validators
            .SelectMany(v => v.Validate(JsonNode.Parse(WithExtras), ValueTypes.Json, new OEmbedPickerConfiguration { Multiple = true }, PropertyValidationContext.Empty()))
            .ToList();

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void A_Value_Without_A_Valid_Type_Is_Not_Valid()
    {
        var results = Editor().Validators
            .SelectMany(v => v.Validate(JsonNode.Parse("""[{ "url": "https://a.test/1", "oembed": { "title": "x" } }]"""), ValueTypes.Json, new OEmbedPickerConfiguration { Multiple = true }, PropertyValidationContext.Empty()))
            .ToList();

        Assert.That(results, Is.Not.Empty);
    }
}
