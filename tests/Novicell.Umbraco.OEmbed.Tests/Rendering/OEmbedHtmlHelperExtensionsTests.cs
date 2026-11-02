using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Novicell.Umbraco.OEmbed.Models;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Extensions;

namespace Novicell.Umbraco.OEmbed.Tests.Rendering;

/// <summary>
/// <c>Html.OEmbed</c> writes the provider's html as it is or nothing, and never anything of its own.
/// </summary>
[TestFixture]
public class OEmbedHtmlHelperExtensionsTests
{
    private const string Iframe = """<iframe width="200" height="113" src="https://www.youtube.com/embed/abc?feature=oembed" frameborder="0" allowfullscreen title="A video"></iframe>""";
    private const string Tweet = """<blockquote class="twitter-tweet"><p>hello</p></blockquote><script async src="https://platform.twitter.com/widgets.js"></script>""";

    private readonly IHtmlHelper _html = Substitute.For<IHtmlHelper>();

    private static OEmbedValue Embed(string? html, OEmbedType type = OEmbedType.Video) => new()
    {
        Url = new Uri("https://example.com/watch?v=abc"),
        OEmbed = new OEmbedResponse { Type = type, Html = html },
    };

    private static string Render(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    [Test]
    public void A_Safe_Iframe_Is_Written_Exactly_As_The_Provider_Returned_It()
        => Assert.That(Render(_html.OEmbed(Embed(Iframe))), Is.EqualTo(Iframe));

    [Test]
    public void Nothing_Is_Added_Or_Trimmed_Around_It()
    {
        var html = "\n  " + Iframe + "\n";

        Assert.That(Render(_html.OEmbed(Embed(html))), Is.EqualTo(html));
    }

    [Test]
    public void Markup_With_A_Script_Is_Left_Out_By_Default()
    {
        Assert.That(Render(_html.OEmbed(Embed(Tweet, OEmbedType.Rich))), Is.Empty);
        Assert.That(Render(_html.OEmbed(Embed(Tweet, OEmbedType.Rich), new OEmbedRenderOptions())), Is.Empty);
        Assert.That(Render(_html.OEmbed(Embed(Tweet, OEmbedType.Rich), new OEmbedRenderOptions { Scripts = OEmbedScripts.Block })), Is.Empty);
    }

    [Test]
    public void An_Iframe_With_An_Event_Handler_Is_Left_Out()
        => Assert.That(Render(_html.OEmbed(Embed("""<iframe src="https://example.com/e" onload="alert(1)"></iframe>"""))), Is.Empty);

    [Test]
    public void Raw_Writes_Any_Html_As_It_Is()
    {
        var options = new OEmbedRenderOptions { Scripts = OEmbedScripts.Raw };

        Assert.That(Render(_html.OEmbed(Embed(Tweet, OEmbedType.Rich), options)), Is.EqualTo(Tweet));
        Assert.That(Render(_html.OEmbed(Embed(Iframe), options)), Is.EqualTo(Iframe));
    }

    private void RequestTo(string host)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        _html.ViewContext.Returns(new ViewContext { HttpContext = httpContext });
    }

    [Test]
    public void An_Iframe_Of_The_Sites_Own_Host_Is_Left_Out_By_Default()
    {
        RequestTo("site.test");
        var own = """<iframe src="https://site.test/media/evil.svg" width="200" height="100"></iframe>""";

        Assert.That(Render(_html.OEmbed(Embed(own))), Is.Empty);

        // Another host is written, and Raw writes anything.
        Assert.That(Render(_html.OEmbed(Embed(Iframe))), Is.EqualTo(Iframe));
        Assert.That(Render(_html.OEmbed(Embed(own), new OEmbedRenderOptions { Scripts = OEmbedScripts.Raw })), Is.EqualTo(own));
    }

    [Test]
    public void The_Host_Of_The_Request_Is_Used_Whatever_Its_Case_Or_Port()
    {
        RequestTo("Site.Test:8443");

        Assert.That(Render(_html.OEmbed(Embed("""<iframe src="https://site.test/e"></iframe>"""))), Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void An_Embed_Without_Html_Gives_Nothing_Whatever_The_Options_Are(string? html)
    {
        Assert.That(Render(_html.OEmbed(Embed(html, OEmbedType.Photo))), Is.Empty);
        Assert.That(Render(_html.OEmbed(Embed(html, OEmbedType.Photo), new OEmbedRenderOptions { Scripts = OEmbedScripts.Raw })), Is.Empty);
    }

    [Test]
    public void A_Missing_Embed_Is_An_Error()
        => Assert.Throws<ArgumentNullException>(() => _html.OEmbed(null!));

    [Test]
    public void The_Default_Is_To_Block()
        => Assert.That(new OEmbedRenderOptions().Scripts, Is.EqualTo(OEmbedScripts.Block));
}
