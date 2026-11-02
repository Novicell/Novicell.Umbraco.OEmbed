using Novicell.Umbraco.OEmbed.Rendering;
using NUnit.Framework;

namespace Novicell.Umbraco.OEmbed.Tests.Rendering;

/// <summary>
/// The html of a saved embed is posted by the backoffice, so a site only writes it as it is when it is one
/// ordinary iframe. These are the iframes real providers return, and the ways of getting something else
/// onto a page that the check has to refuse.
/// </summary>
[TestFixture]
public class OEmbedHtmlGateTests
{
    // What the providers returned, copied from the test site's sample page and from the providers.
    private static readonly string[] Providers =
    [
        """<iframe width="200" height="150" src="https://www.youtube.com/embed/jNQXAC9IVRw?feature=oembed" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen title="Me at the zoo"></iframe>""",
        """<iframe width="200" height="113" src="https://www.youtube.com/embed/9bZkp7q19f0?feature=oembed" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen title="PSY - GANGNAM STYLE(강남스타일) M/V"></iframe>""",
        """<iframe src="https://player.vimeo.com/video/371827688?app_id=122963" width="426" height="240" frameborder="0" allow="autoplay; fullscreen; picture-in-picture; clipboard-write; encrypted-media; web-share" referrerpolicy="strict-origin-when-cross-origin" title="Umbraco The Friendly CMS"></iframe>""",
        """<iframe width="100%" height="400" scrolling="no" frameborder="no" allow="autoplay; encrypted-media" src="https://w.soundcloud.com/player/?visual=true&url=https%3A%2F%2Fapi.soundcloud.com%2Ftracks%2F293&show_artwork=true"></iframe>""",
        """<iframe style="border-radius: 12px" width="100%" height="152" title="Spotify Embed: Knights of Cydonia" frameborder="0" allowfullscreen allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture" loading="lazy" src="https://open.spotify.com/embed/track/7ouMYWpwJ422jRcDASZB7P?utm_source=oembed"></iframe>""",
        """<iframe frameborder="0" width="480" height="270" src="https://www.dailymotion.com/embed/video/x7tgad0" allowfullscreen allow="autoplay; fullscreen; picture-in-picture; web-share"></iframe>""",
        // As the providers write them in other places: line breaks between the attributes, a trailing line break, upper case.
        "<iframe\n  src=\"https://example.com/e/1?a=1&amp;b=2\"\n  width=\"560\"\n  height=\"315\"></iframe>\n",
        """<IFRAME SRC="https://example.com/e/2" WIDTH="560" HEIGHT="315"></IFRAME>""",
        """<iframe src='https://example.com/e/3' title='Single quotes'></iframe>""",
    ];

    [TestCaseSource(nameof(Providers))]
    public void What_Real_Providers_Return_Is_Allowed(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.True);

    // Markup that is not one iframe: the scripts of X, TikTok and Reddit, and everything around an iframe.
    [TestCase("""<blockquote class="twitter-tweet"><p>hello</p></blockquote><script async src="https://platform.twitter.com/widgets.js"></script>""")]
    [TestCase("""<script src="https://example.com/x.js"></script>""")]
    [TestCase("""<iframe src="https://example.com/e"></iframe><script>alert(1)</script>""")]
    [TestCase("""<script>alert(1)</script><iframe src="https://example.com/e"></iframe>""")]
    [TestCase("""text <iframe src="https://example.com/e"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e"></iframe> text""")]
    [TestCase("""<div><iframe src="https://example.com/e"></iframe></div>""")]
    [TestCase("""<iframe src="https://example.com/a"></iframe><iframe src="https://example.com/b"></iframe>""")]
    [TestCase("""<!-- --><iframe src="https://example.com/e"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e"><script>alert(1)</script></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e">fallback text</iframe>""")]
    [TestCase("""<img src=x onerror=alert(1)>""")]
    public void Anything_That_Is_Not_One_Iframe_Is_Refused(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.False);

    // An iframe that is not closed or is self-closed swallows the page that follows it.
    [TestCase("""<iframe src="https://example.com/e">""")]
    [TestCase("""<iframe src="https://example.com/e"/>""")]
    [TestCase("""<iframe src="https://example.com/e" />""")]
    [TestCase("""<iframe src="https://example.com/e"></iframe""")]
    public void An_Iframe_That_Is_Not_Closed_Properly_Is_Refused(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.False);

    // Every way of running a script or loading other markup from an iframe tag.
    [TestCase("""<iframe src="https://example.com/e" onload="alert(1)"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" ONLOAD="alert(1)"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" onerror=alert(1)></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" onmouseover="alert(1)"></iframe>""")]
    [TestCase("""<iframe srcdoc="<script>alert(1)</script>"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" srcdoc="<script>alert(1)</script>"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" formaction="javascript:alert(1)"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" id="x"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" class="x"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" name="top"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" data-bs-toggle="modal"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" csp="default-src 'none'"></iframe>""")]
    public void An_Attribute_That_Is_Not_On_The_List_Is_Refused(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.False);

    [TestCase("""<iframe src="javascript:alert(1)"></iframe>""")]
    [TestCase("""<iframe src="JavaScript:alert(1)"></iframe>""")]
    [TestCase("""<iframe src="data:text/html,<script>alert(1)</script>"></iframe>""")]
    [TestCase("""<iframe src="vbscript:msgbox(1)"></iframe>""")]
    [TestCase("""<iframe src="blob:https://example.com/x"></iframe>""")]
    [TestCase("""<iframe src="/relative/path"></iframe>""")]
    [TestCase("""<iframe src="//example.com/e"></iframe>""")]
    [TestCase("""<iframe src="example.com/e"></iframe>""")]
    [TestCase("""<iframe src=""></iframe>""")]
    [TestCase("""<iframe src=https://example.com/e&#58;x></iframe>""")]
    [TestCase("""<iframe src="&#106;avascript:alert(1)"></iframe>""")]
    [TestCase("""<iframe src="&#x6A;avascript:alert(1)"></iframe>""")]
    [TestCase("""<iframe src="java&Tab;script:alert(1)"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e&quot; onload=&quot;alert(1)"></iframe>""")]
    [TestCase("""<iframe src=" https://example.com/e"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e "></iframe>""")]
    [TestCase("""<iframe src="https://exa&#109;ple.com/e"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e?x=1&copy=2&notit"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e&sect"></iframe>""")]
    [TestCase("""<iframe width="560" height="315"></iframe>""")]
    // A browser reads these as a path on the site itself (https:/media/x is https://site/media/x on an https page),
    // or as another host than a parser does, so they are refused whatever the site's host is.
    [TestCase("""<iframe src="https:/media/evil.svg"></iframe>""")]
    [TestCase("""<iframe src="HTTPS:/media/evil.svg"></iframe>""")]
    [TestCase("""<iframe src="https:media/evil.svg"></iframe>""")]
    [TestCase("""<iframe src="https:///site.test/media/evil.svg"></iframe>""")]
    [TestCase("""<iframe src="https:\\site.test\media\evil.svg"></iframe>""")]
    [TestCase("""<iframe src="https:/\site.test/media/evil.svg"></iframe>""")]
    [TestCase("""<iframe src="https://site.test\@evil.example/e"></iframe>""")]
    [TestCase("""<iframe src="https://user:pass@example.com/e"></iframe>""")]
    [TestCase("""<iframe src="https://example.com\e"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e\x"></iframe>""")]
    [TestCase("""<iframe src="https://"></iframe>""")]
    [TestCase("""<iframe src="https:///"></iframe>""")]
    public void An_Address_That_Is_Not_Absolute_Http_Is_Refused(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.False);

    [TestCase("""<iframe src="https://example.com/a" src="https://example.com/b"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/a" SRC="javascript:alert(1)"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/a" width="1" width="2"></iframe>""")]
    public void An_Attribute_Given_Twice_Is_Refused(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.False);

    // The ways of writing it that a browser reads differently from a strict match.
    [TestCase("""<iframe src="https://example.com/e"onload="alert(1)"></iframe>""")]
    [TestCase("""<iframe/src="https://example.com/e"/onload="alert(1)"></iframe>""")]
    [TestCase("<iframe src=\"https://example.com/e\" onload=\"alert(1)\"></iframe>")]
    [TestCase("<iframe src=\"https://example.com/e\"></iframe>")]
    [TestCase(" <iframe src=\"https://example.com/e\"></iframe>")]
    [TestCase("<iframe src=\"https://example.com/e\"\u000Bonload=\"alert(1)\"></iframe>")]
    [TestCase("""<iframe src="https://example.com/e" title="a" b"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" title="x"y"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" title="<script>"></iframe>""")]
    [TestCase("""<iframe src=`https://example.com/e`></iframe>""")]
    [TestCase("<iframe src=\"https://example.com/e\" title=\"a\u0000b\"></iframe>")]
    [TestCase("<iframe src=\"https://exa\tmple.com/e\"></iframe>")]
    [TestCase("<iframe src=\"https://example.com/e\" title=\"a\nb\"></iframe>")]
    public void Markup_A_Browser_Could_Read_Differently_Is_Refused(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.False);

    [TestCase("""<iframe src="https://example.com/e" width="100px"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" width="calc(100%)"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" height="-1"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" loading="x"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" frameborder="javascript:alert(1)"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" referrerpolicy="x"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" allow="camera https://evil.test"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" allow="camera 'src'"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" sandbox="allow-scripts; x"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" allowfullscreen="x"></iframe>""")]
    public void A_Value_Of_An_Unexpected_Shape_Is_Refused(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.False);

    // A style may only lay the frame out, never put it over the page or load anything.
    [TestCase("""<iframe src="https://example.com/e" style="position:fixed;top:0;left:0;width:100%;height:100%"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="position: absolute"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="z-index:99999"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="background:url(https://evil.test/x)"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="width:expression(alert(1))"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="width:100% !important"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="border-radius"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="pointer-events:none"></iframe>""")]
    // A negative margin or vertical-align moves the frame: a full-page frame is put over the controls above it.
    [TestCase("""<iframe src="https://example.com/e" style="display:block;width:100vw;height:100vh;margin:-100vh 0 0"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="margin: -100vh 0 0"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="margin:0"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="vertical-align: -9999px"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="vertical-align: middle"></iframe>""")]
    public void A_Style_That_Is_Not_Only_Layout_Is_Refused(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.False);

    [TestCase("""<iframe src="https://example.com/e" style="border-radius: 12px"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style="border:0;width:100%;aspect-ratio:16 / 9"></iframe>""")]
    [TestCase("""<iframe src="https://example.com/e" style=""></iframe>""")]
    public void A_Layout_Style_Is_Allowed(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.True);

    // A frame of the site's own host is not isolated from the page: a script in what it loads, an SVG uploaded to
    // the site for example, could reach the page.
    [TestCase("""<iframe src="https://site.test/media/evil.svg"></iframe>""")]
    [TestCase("""<iframe src="https://SITE.test/media/evil.svg"></iframe>""")]
    [TestCase("""<iframe src="http://site.test/media/evil.svg"></iframe>""")]
    [TestCase("""<iframe src="https://site.test:8443/media/evil.svg"></iframe>""")]
    [TestCase("""<iframe src="https://user@site.test/media/evil.svg"></iframe>""")]
    public void An_Iframe_Of_The_Sites_Own_Host_Is_Refused(string html)
    {
        Assert.That(OEmbedHtmlGate.IsSafeIframe(html, siteHost: "site.test"), Is.False);
        Assert.That(OEmbedHtmlGate.IsSafeIframe(html, siteHost: "SITE.TEST"), Is.False);
    }

    // The same host in the spellings a browser treats as one: the Unicode and the punycode form of a name, upper
    // case, and a trailing dot.
    [TestCase("site.test", "https://site.test./media/evil.svg")]
    [TestCase("site.test.", "https://site.test/media/evil.svg")]
    [TestCase("xn--bcher-kva.example", "https://bücher.example/media/evil.svg")]
    [TestCase("xn--bcher-kva.example", "https://BÜCHER.example/media/evil.svg")]
    [TestCase("bücher.example", "https://xn--bcher-kva.example/media/evil.svg")]
    [TestCase("BÜCHER.example", "https://xn--bcher-kva.example/media/evil.svg")]
    [TestCase("xn--bcher-kva.example", "https://xn--bcher-kva.example/media/evil.svg")]
    [TestCase("127.0.0.1", "https://127.0.0.1:44366/media/evil.svg")]
    [TestCase("127.0.0.1", "https://2130706433/media/evil.svg")]
    [TestCase("[::1]", "https://[::1]:44366/media/evil.svg")]
    [TestCase("::1", "https://[::1]/media/evil.svg")]
    [TestCase("[::1]", "https://[0:0:0:0:0:0:0:1]/media/evil.svg")]
    [TestCase("[0:0:0:0:0:0:0:1]", "https://[::1]/media/evil.svg")]
    [TestCase("[0000:0000:0000:0000:0000:0000:0000:0001]", "https://[::1]/media/evil.svg")]
    [TestCase("[2001:db8::1]", "https://[2001:0DB8:0:0:0:0:0:1]/media/evil.svg")]
    [TestCase("[2001:DB8::1]", "https://[2001:db8:0:0:0:0:0:1]/media/evil.svg")]
    [TestCase("[::ffff:127.0.0.1]", "https://[::ffff:7f00:1]/media/evil.svg")]
    [TestCase("127.0.0.1", "https://0x7f.1/media/evil.svg")]
    public void The_Same_Host_Is_Refused_In_Every_Spelling(string siteHost, string src)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe($"""<iframe src="{src}"></iframe>""", siteHost), Is.False);

    [TestCase("xn--bcher-kva.example", "https://xn--bchr-kva.example/e")]
    [TestCase("bücher.example", "https://bücher.example.evil.test/e")]
    [TestCase("site.test", "https://sitextest/e")]
    [TestCase("127.0.0.1", "https://127.0.0.2/e")]
    [TestCase("not a valid host!", "https://example.com/e")]
    public void A_Different_Host_Is_Allowed_And_A_Host_That_Cannot_Be_Read_Does_Not_Fail_The_Check(string siteHost, string src)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe($"""<iframe src="{src}"></iframe>""", siteHost), Is.True);

    [Test]
    public void The_Own_Host_Is_Only_Refused_When_It_Is_Known()
        => Assert.That(OEmbedHtmlGate.IsSafeIframe("""<iframe src="https://site.test/media/evil.svg"></iframe>"""), Is.True);

    // Other hosts are not the site's, a subdomain is a different origin and a name that only ends the same is another host.
    [TestCase("""<iframe src="https://example.com/e"></iframe>""")]
    [TestCase("""<iframe src="https://player.vimeo.com/video/1"></iframe>""")]
    [TestCase("""<iframe src="https://www.site.test/e"></iframe>""")]
    [TestCase("""<iframe src="https://othersite.test/e"></iframe>""")]
    [TestCase("""<iframe src="https://site.test.evil.example/e"></iframe>""")]
    public void An_Iframe_Of_Another_Host_Is_Allowed_Whatever_The_Sites_Host_Is(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html, siteHost: "site.test"), Is.True);

    [TestCaseSource(nameof(Providers))]
    public void The_Providers_Are_Allowed_On_A_Site_With_Any_Host_Of_Its_Own(string html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html, siteHost: "site.test"), Is.True);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not html")]
    public void Nothing_Usable_Is_Refused(string? html)
        => Assert.That(OEmbedHtmlGate.IsSafeIframe(html), Is.False);

    [Test]
    public void A_Long_Value_Of_The_Wrong_Shape_Is_Refused_Quickly()
    {
        // The check is a match against the whole text, so a long or nested value must not make it slow.
        var attributes = string.Concat(Enumerable.Repeat(" a=\"b\"", 5000));
        var html = $"<iframe src=\"https://example.com/e\"{attributes} onload=x></iframe>";

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var result = OEmbedHtmlGate.IsSafeIframe(html);
        watch.Stop();

        Assert.That(result, Is.False);
        Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
    }
}
