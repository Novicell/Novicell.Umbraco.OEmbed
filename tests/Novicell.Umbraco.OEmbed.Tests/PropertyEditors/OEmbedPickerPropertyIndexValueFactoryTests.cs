using Novicell.Umbraco.OEmbed.PropertyEditors;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.Tests.PropertyEditors;

/// <summary>
/// What a search is given for an embed: the words that describe it, not the stored JSON with the provider's html.
/// </summary>
[TestFixture]
public class OEmbedPickerPropertyIndexValueFactoryTests
{
    private const string TwoEmbeds = """
        [
          { "url": "https://www.youtube.com/watch?v=abc123",
            "oembed": { "type": "video", "version": "1.0", "title": "Mountain sunrise", "author_name": "Alice Hiker",
                        "provider_name": "YouTube", "width": 200, "height": 113,
                        "html": "<iframe src=\"https://www.youtube.com/embed/abc123\"></iframe>" } },
          { "url": "https://vimeo.com/42",
            "oembed": { "type": "video", "title": "City at night", "author_name": "Bob Walker", "provider_name": "Vimeo",
                        "description": "Timelapse of the harbour", "html": "<iframe src=\"https://player.vimeo.com/video/42\"></iframe>" } }
        ]
        """;

    private static IProperty Property(object? value, string alias = "embeds")
    {
        IProperty property = Substitute.For<IProperty>();
        property.Alias.Returns(alias);
        property.GetValue(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>()).Returns(value);
        return property;
    }

    private static List<IndexValue> Index(IProperty property, string? culture = null)
        => new OEmbedPickerPropertyIndexValueFactory()
            .GetIndexValues(property, culture, null, false, [], new Dictionary<Guid, IContentType>())
            .ToList();

    private static string[] Texts(IndexValue value) => value.Values.Cast<string>().ToArray();

    [Test]
    public void Each_Embed_Is_One_Text_With_The_Words_It_Is_Found_By()
    {
        List<IndexValue> result = Index(Property(TwoEmbeds));

        Assert.That(result, Has.Count.EqualTo(1));
        string[] texts = Texts(result[0]);
        Assert.That(texts, Has.Length.EqualTo(2));
        Assert.That(texts[0], Does.Contain("Mountain sunrise").And.Contain("Alice Hiker").And.Contain("YouTube").And.Contain("https://www.youtube.com/watch?v=abc123"));
        Assert.That(texts[1], Does.Contain("City at night").And.Contain("Bob Walker").And.Contain("Vimeo").And.Contain("Timelapse of the harbour").And.Contain("https://vimeo.com/42"));
    }

    [Test]
    public void The_Markup_And_The_Technical_Fields_Are_Not_Indexed()
    {
        string all = string.Join(" ", Texts(Index(Property(TwoEmbeds))[0]));

        // These words are in every embed, so a search for them would find all of them.
        Assert.That(all, Does.Not.Contain("iframe").And.Not.Contain("oembed").And.Not.Contain("version").And.Not.Contain("width").And.Not.Contain("html"));
        Assert.That(all, Does.Not.Contain("player.vimeo.com"), "the html is left out");
    }

    [Test]
    public void The_Field_Is_The_Property_Alias_And_The_Culture_Is_Passed_On()
    {
        IndexValue value = Index(Property(TwoEmbeds, alias: "video"), culture: "da")[0];

        Assert.That(value.FieldName, Is.EqualTo("video"));
        Assert.That(value.Culture, Is.EqualTo("da"));
    }

    [Test]
    public void A_Value_From_The_Umbraco_9_Version_Is_Read_Too()
    {
        List<IndexValue> result = Index(Property("""{ "url": "https://youtu.be/x", "oembed": { "type": "video", "title": "Old value", "provider_name": "YouTube" } }"""));

        Assert.That(Texts(result[0]), Has.Length.EqualTo(1));
        Assert.That(Texts(result[0])[0], Does.Contain("Old value"));
    }

    [Test]
    public void An_Embed_Without_A_Title_Is_Still_Found_By_Its_Url()
    {
        string[] texts = Texts(Index(Property("""[{ "url": "https://example.com/p/1", "oembed": { "type": "rich" } }]"""))[0]);

        Assert.That(texts, Is.EqualTo(new[] { "https://example.com/p/1" }));
    }

    [Test]
    public void An_Item_That_Cannot_Be_Used_Is_Left_Out_And_The_Others_Are_Kept()
    {
        string[] texts = Texts(Index(Property("""
            [ { "url": "https://a.test/1" },
              null,
              { "url": "https://a.test/2", "oembed": { "type": "video", "title": "Usable" } } ]
            """))[0]);

        Assert.That(texts, Has.Length.EqualTo(1));
        Assert.That(texts[0], Does.Contain("Usable"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not json")]
    [TestCase("[]")]
    [TestCase("{}")]
    [TestCase("""[{ "url": "https://a.test/1" }]""")]
    [TestCase(42)]
    public void Nothing_Usable_Gives_Nothing_And_Does_Not_Throw(object? value)
        => Assert.That(Index(Property(value)), Is.Empty);

    [Test]
    public void The_Editor_Uses_The_Factory()
    {
        var editor = new OEmbedPickerPropertyEditor(Substitute.For<IDataValueEditorFactory>(), Substitute.For<IIOHelper>());

        Assert.That(editor.PropertyIndexValueFactory, Is.TypeOf<OEmbedPickerPropertyIndexValueFactory>());
    }
}
