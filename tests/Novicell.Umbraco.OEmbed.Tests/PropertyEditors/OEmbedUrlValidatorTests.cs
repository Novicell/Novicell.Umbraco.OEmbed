using System.ComponentModel.DataAnnotations;
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
public class OEmbedUrlValidatorTests
{
    private ILocalizedTextService _localizedTextService = null!;
    private OEmbedPickerPropertyValueEditor.UrlValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _localizedTextService = Substitute.For<ILocalizedTextService>();
        _localizedTextService
            .Localize(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CultureInfo?>(), Arg.Any<IDictionary<string, string?>?>())
            .Returns("localized message");
        _validator = new OEmbedPickerPropertyValueEditor.UrlValidator(_localizedTextService);
    }

    private static OEmbedDto Embed(string url = "https://youtube.com/watch?v=1", Action<OEmbedResponse>? configure = null)
    {
        var response = new OEmbedResponse { Type = OEmbedType.Video };
        configure?.Invoke(response);
        return new OEmbedDto { Url = url, OEmbed = response };
    }

    private IEnumerable<ValidationResult> Validate(params OEmbedDto[] values)
        => _validator.Validate([.. values], new OEmbedPickerConfiguration(), null, PropertyValidationContext.Empty());

    [Test]
    public void Null_Value_Is_Not_Validated()
        => Assert.That(
            _validator.Validate(null, new OEmbedPickerConfiguration(), null, PropertyValidationContext.Empty()),
            Is.Empty);

    [Test]
    public void Http_And_Https_Urls_Pass()
    {
        Assert.That(
            Validate(
                Embed("http://example.com/a"),
                Embed("https://example.com/a", r =>
                {
                    r.Url = new Uri("https://example.com/photo.jpg");
                    r.AuthorUrl = new Uri("https://example.com/author");
                    r.ProviderUrl = new Uri("http://example.com");
                    r.ThumbnailUrl = new Uri("https://example.com/thumb.jpg");
                })),
            Is.Empty);
    }

    [Test]
    public void Relative_Urls_In_The_Response_Pass()
    {
        // Some providers return a relative thumbnail; it cannot carry a script scheme.
        Assert.That(
            Validate(Embed(configure: r =>
            {
                r.ThumbnailUrl = new Uri("/thumb.jpg", UriKind.Relative);
                r.AuthorUrl = new Uri("//example.com/author", UriKind.Relative);
            })),
            Is.Empty);
    }

    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,<script>alert(1)</script>")]
    [TestCase("file:///etc/passwd")]
    [TestCase("ftp://example.com/x")]
    [TestCase("/relative/path")]
    [TestCase("not a url")]
    [TestCase("")]
    public void The_Url_The_Editor_Entered_Must_Be_Absolute_Http_Or_Https(string url)
        => Assert.That(Validate(Embed(url)), Is.Not.Empty);

    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,x")]
    [TestCase("file:///etc/passwd")]
    [TestCase("vbscript:msgbox(1)")]
    public void A_Non_Http_Absolute_Url_In_The_Response_Is_Rejected_In_Every_Field(string url)
    {
        var bad = new Uri(url);

        Assert.That(Validate(Embed(configure: r => r.Url = bad)), Is.Not.Empty);
        Assert.That(Validate(Embed(configure: r => r.AuthorUrl = bad)), Is.Not.Empty);
        Assert.That(Validate(Embed(configure: r => r.ProviderUrl = bad)), Is.Not.Empty);
        Assert.That(Validate(Embed(configure: r => r.ThumbnailUrl = bad)), Is.Not.Empty);
    }

    // Browsers strip tab, newline and carriage return anywhere in a URL, and leading control
    // characters, so these become javascript: when rendered as a link even though System.Uri sees
    // a relative URI.
    [TestCase("java\nscript:alert(1)")]
    [TestCase("java\tscript:alert(1)")]
    [TestCase("jav\rascript:alert(1)")]
    [TestCase("\u0001javascript:alert(1)")]
    [TestCase("\u001fjavascript:alert(1)")]
    [TestCase("\u007fjavascript:alert(1)")]
    [TestCase("/path\n")]
    [TestCase("data\n:text/html,x")]
    public void A_Url_With_Control_Characters_Is_Rejected_Even_When_It_Looks_Relative(string url)
    {
        var uri = new Uri(url, UriKind.RelativeOrAbsolute);

        Assert.That(Validate(Embed(configure: r => r.AuthorUrl = uri)), Is.Not.Empty);
        Assert.That(Validate(Embed(configure: r => r.ProviderUrl = uri)), Is.Not.Empty);
        Assert.That(Validate(Embed(configure: r => r.ThumbnailUrl = uri)), Is.Not.Empty);
        Assert.That(Validate(Embed(configure: r => r.Url = uri)), Is.Not.Empty);
    }

    [TestCase("https://example.com/a\nb")]
    [TestCase("https://example.com/\u0001")]
    [TestCase("https://exam\tple.com/")]
    public void The_Url_The_Editor_Entered_Cannot_Contain_Control_Characters(string url)
        => Assert.That(Validate(Embed(url)), Is.Not.Empty);

    [Test]
    public void Explicit_Nulls_In_The_Posted_Value_Fail_The_Value_Without_Throwing()
    {
        // `required` does not stop an explicit JSON null: [null], or "oembed": null.
        Assert.That(
            _validator.Validate([null!], new OEmbedPickerConfiguration(), null, PropertyValidationContext.Empty()),
            Is.Not.Empty);

        var nullResponse = new OEmbedDto { Url = "https://youtube.com/watch?v=1", OEmbed = null };
        Assert.That(Validate(nullResponse), Is.Not.Empty);

        var nullUrl = new OEmbedDto { Url = null, OEmbed = new OEmbedResponse { Type = OEmbedType.Video } };
        Assert.That(Validate(nullUrl), Is.Not.Empty);
    }

    [Test]
    public void One_Bad_Item_Among_Several_Fails_The_Value()
        => Assert.That(
            Validate(Embed(), Embed("javascript:alert(1)"), Embed()),
            Is.Not.Empty);

    [Test]
    public void Reuses_Umbracos_InvalidPattern_Message_And_Reports_Against_The_Value()
    {
        var results = Validate(Embed("javascript:alert(1)")).ToList();

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].MemberNames, Is.EqualTo(new[] { "value" }));
        _localizedTextService.Received(1).Localize(
            "validation",
            "invalidPattern",
            Arg.Any<CultureInfo?>(),
            Arg.Any<IDictionary<string, string?>?>());
    }
}
