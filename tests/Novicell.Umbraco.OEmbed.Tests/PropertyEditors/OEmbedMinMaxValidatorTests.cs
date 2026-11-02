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
public class OEmbedMinMaxValidatorTests
{
    private ILocalizedTextService _localizedTextService = null!;
    private OEmbedPickerPropertyValueEditor.MinMaxValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _localizedTextService = Substitute.For<ILocalizedTextService>();
        _localizedTextService
            .Localize(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CultureInfo?>(), Arg.Any<IDictionary<string, string?>?>())
            .Returns("localized message");
        _validator = new OEmbedPickerPropertyValueEditor.MinMaxValidator(_localizedTextService);
    }

    private static OEmbedValue Video(string url) => new()
    {
        Url = new Uri(url),
        OEmbed = new OEmbedResponse { Type = OEmbedType.Video },
    };

    private static OEmbedPickerConfiguration Config(int? min = null, int? max = null, bool multiple = true) => new()
    {
        Multiple = multiple,
        ValidationLimit = new OEmbedPickerConfiguration.NumberRange { Min = min, Max = max },
    };

    private static PropertyValidationContext Context() => PropertyValidationContext.Empty();

    [Test]
    public void Null_Value_Or_Configuration_Is_Not_Validated()
    {
        Assert.That(_validator.Validate(null, Config(min: 1), null, Context()), Is.Empty);
        Assert.That(_validator.Validate(OEmbedDtos.Of([Video("https://youtube.com/watch?v=1")]), null, null, Context()), Is.Empty);
    }

    [Test]
    public void Item_Count_Within_Min_And_Max_Passes()
    {
        List<OEmbedValue> value = [Video("https://youtube.com/watch?v=1"), Video("https://youtube.com/watch?v=2")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(min: 1, max: 2, multiple: true), null, Context());

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Item_Count_Below_Min_Uses_The_Shared_EntriesShort_Localization_Key()
    {
        // Reuses Umbraco's own "validation"/"entriesShort" key/tokens (min, shortfall),
        // same as Media Picker 3's MinMaxValidator, instead of a hardcoded English string.
        List<OEmbedValue> value = [Video("https://youtube.com/watch?v=1")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(min: 2), null, Context());

        Assert.That(results, Is.Not.Empty);
        _localizedTextService.Received(1).Localize(
            "validation",
            "entriesShort",
            Arg.Any<CultureInfo?>(),
            Arg.Is<IDictionary<string, string?>>(d => d["0"] == "2" && d["1"] == "1"));
    }

    [Test]
    public void Item_Count_Above_Max_Uses_The_Shared_EntriesExceed_Localization_Key()
    {
        // Reuses Umbraco's own "validation"/"entriesExceed" key/tokens (max, excess),
        // same as Media Picker 3's MinMaxValidator, instead of a hardcoded English string.
        List<OEmbedValue> value = [Video("https://youtube.com/watch?v=1"), Video("https://youtube.com/watch?v=2")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(max: 1), null, Context());

        Assert.That(results, Is.Not.Empty);
        _localizedTextService.Received(1).Localize(
            "validation",
            "entriesExceed",
            Arg.Any<CultureInfo?>(),
            Arg.Is<IDictionary<string, string?>>(d => d["0"] == "1" && d["1"] == "1"));
    }

    [Test]
    public void Multiple_False_With_More_Than_One_Item_Uses_The_Shared_MultipleMediaNotAllowed_Localization_Key()
    {
        // Reuses Umbraco's own "validation"/"multipleMediaNotAllowed" key, same as Media Picker 3's
        // MinMaxValidator, instead of a hardcoded English string.
        List<OEmbedValue> value = [Video("https://youtube.com/watch?v=1"), Video("https://youtube.com/watch?v=2")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(multiple: false), null, Context());

        Assert.That(results, Is.Not.Empty);
        _localizedTextService.Received(1).Localize(
            "validation",
            "multipleMediaNotAllowed",
            Arg.Any<CultureInfo?>(),
            Arg.Any<IDictionary<string, string?>?>());
    }

    [Test]
    public void Multiple_False_With_One_Item_Passes()
    {
        List<OEmbedValue> value = [Video("https://youtube.com/watch?v=1")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(multiple: false), null, Context());

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Missing_ValidationLimit_Configuration_Skips_Min_Max_Checks()
    {
        List<OEmbedValue> value = [Video("https://youtube.com/watch?v=1")];

        var results = _validator.Validate(OEmbedDtos.Of(value), Config(multiple: true), null, Context());

        Assert.That(results, Is.Empty);
    }
}
