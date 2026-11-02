using System.Globalization;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using Novicell.Umbraco.OEmbed.Tests.TestSupport;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Models.Validation;
using Umbraco.Cms.Core.Services;

namespace Novicell.Umbraco.OEmbed.Tests.PropertyEditors;

[TestFixture]
public class OEmbedAllowedTypeValidatorTests
{
    private ILocalizedTextService _localizedTextService = null!;
    private OEmbedPickerPropertyValueEditor.AllowedTypeValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _localizedTextService = Substitute.For<ILocalizedTextService>();
        _localizedTextService
            .Localize(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CultureInfo?>(), Arg.Any<IDictionary<string, string?>?>())
            .Returns("localized message");
        _validator = new OEmbedPickerPropertyValueEditor.AllowedTypeValidator(_localizedTextService);
    }

    private static OEmbedValue Embed(OEmbedType type, string url) => new()
    {
        Url = new Uri(url),
        OEmbed = new OEmbedResponse { Type = type },
    };

    private static OEmbedPickerConfiguration Config(string[]? allowedTypes = null) => new()
    {
        Multiple = true,
        AllowedTypes = allowedTypes ?? [],
    };

    private static PropertyValidationContext Context() => PropertyValidationContext.Empty();

    [Test]
    public void Null_Value_Or_Configuration_Is_Not_Validated()
    {
        Assert.That(_validator.Validate(null, Config(allowedTypes: ["video"]), null, Context()), Is.Empty);
        Assert.That(_validator.Validate(OEmbedDtos.Of([Embed(OEmbedType.Video, "https://youtube.com/watch?v=1")]), null, null, Context()), Is.Empty);
    }

    [Test]
    public void Allowed_Type_Passes()
    {
        List<OEmbedValue> value = [Embed(OEmbedType.Photo, "https://flickr.com/photo/1")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(allowedTypes: ["photo", "video"]), null, Context());

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Disallowed_Type_Uses_The_Shared_InvalidObjectType_Localization_Key()
    {
        // Reuses Umbraco's own "validation"/"invalidObjectType" key, same as the Multi Node
        // Tree Picker's ObjectTypeValidator, instead of a hardcoded English string.
        List<OEmbedValue> value = [Embed(OEmbedType.Photo, "https://flickr.com/photo/1")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(allowedTypes: ["video"]), null, Context());

        Assert.That(results, Is.Not.Empty);
        _localizedTextService.Received(1).Localize(
            "validation",
            "invalidObjectType",
            Arg.Any<CultureInfo?>(),
            Arg.Any<IDictionary<string, string?>?>());
    }

    [Test]
    public void Disallowed_Type_Produces_One_Result_Per_Invalid_Item()
    {
        List<OEmbedValue> value =
        [
            Embed(OEmbedType.Video, "https://youtube.com/watch?v=1"),
            Embed(OEmbedType.Photo, "https://flickr.com/photo/1"),
        ];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(allowedTypes: ["video"]), null, Context());

        Assert.That(results.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Explicit_Nulls_In_The_Posted_Value_Are_Not_Allowed_And_Do_Not_Throw()
    {
        // `required` does not stop an explicit JSON null: [null], or "oembed": null.
        var nullResponse = new OEmbedValue { Url = new Uri("https://youtube.com/watch?v=1"), OEmbed = null! };
        List<OEmbedValue> value = [null!, nullResponse];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(allowedTypes: ["video"]), null, Context()).ToList();

        Assert.That(results, Has.Count.EqualTo(2));
    }

    [Test]
    public void Empty_AllowedTypes_Means_Unrestricted()
    {
        List<OEmbedValue> value = [Embed(OEmbedType.Photo, "https://flickr.com/photo/1")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(allowedTypes: []), null, Context());

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Allowed_Type_Check_Is_Case_Insensitive()
    {
        List<OEmbedValue> value = [Embed(OEmbedType.Photo, "https://flickr.com/photo/1")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(allowedTypes: ["PHOTO"]), null, Context());

        Assert.That(results, Is.Empty);
    }
}
