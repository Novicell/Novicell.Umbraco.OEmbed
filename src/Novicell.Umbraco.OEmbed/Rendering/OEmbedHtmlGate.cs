using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Novicell.Umbraco.OEmbed.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.Rendering;

/// <summary>
/// Decides whether the html of an embed may be written to a page as it is: exactly one <c>&lt;iframe&gt;</c>
/// with attributes from a short list and values of a known shape, and nothing else.
/// </summary>
/// <remarks>
/// It never changes the html, it only says yes or no, so what a site gets is exactly what the provider returned.
/// The saved html is posted by the client and is not verified (see the README), so the check is strict: it is a
/// match against the whole text, not a parse that a browser could read differently. Only the five characters HTML
/// treats as white space separate attributes, a closing tag is required (<c>&lt;iframe /&gt;</c> would swallow
/// the page after it), and an attribute twice, an unknown attribute (every event handler, <c>srcdoc</c>), anything
/// between the tags and any <c>&amp;</c> that is not one of five plain entities fail it. The embed is isolated by
/// the browser as any cross-origin iframe is, which is why an iframe can be allowed while markup with a script
/// cannot.
/// </remarks>
internal static partial class OEmbedHtmlGate
{
    // The HTML white space characters. \s is wider (it has no-break space and other Unicode spaces).
    private const string Space = "[\\t\\n\\f\\r ]";

    // One attribute: white space, a name, and optionally = and a value that is quoted or a plain token.
    private const string Attribute =
        $"{Space}+(?<name>[A-Za-z][A-Za-z0-9:_-]*)(?:{Space}*={Space}*(?:\"(?<dq>[^\"<>]*)\"|'(?<sq>[^'<>]*)'|(?<uq>[^\\t\\n\\f\\r \"'`=<>]+)))?";

    // The whole text is one iframe: attributes, the closing tag, nothing else.
    [GeneratedRegex($"\\A{Space}*<iframe(?<attributes>(?:{Attribute})*){Space}*>{Space}*</iframe{Space}*>{Space}*\\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Iframe();

    [GeneratedRegex(Attribute, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Attributes();

    [GeneratedRegex("\\A\\d{1,4}%?\\z")]
    private static partial Regex Size();

    [GeneratedRegex("\\A[A-Za-z0-9\\-; ]*\\z")]
    private static partial Regex Permissions();

    [GeneratedRegex("\\A[A-Za-z\\- ]*\\z")]
    private static partial Regex SandboxTokens();

    [GeneratedRegex("\\A[A-Za-z0-9_\\-]*\\z")]
    private static partial Regex TestId();

    [GeneratedRegex("\\A[a-z0-9.%#/\\- ]+\\z", RegexOptions.IgnoreCase)]
    private static partial Regex StyleValue();

    // scheme://host, where the host starts and goes on without / \ ? # @ or white space, then an optional path,
    // query or fragment without a backslash or white space.
    [GeneratedRegex("\\Ahttps?://[^/\\\\?#@\\s][^/\\\\?#@\\s]*(?:[/?#][^\\\\\\s]*)?\\z", RegexOptions.IgnoreCase)]
    private static partial Regex SourceShape();

    // The start of a query parameter after a bare &: a name and =.
    [GeneratedRegex("\\A[A-Za-z0-9_]+=")]
    private static partial Regex Parameter();

    // The only entities allowed in a value; any other & could be read differently by a browser.
    [GeneratedRegex("&(?:amp|quot|lt|gt|#39|#x27);", RegexOptions.IgnoreCase)]
    private static partial Regex Entity();

    private static readonly HashSet<string> ReferrerPolicies = new(StringComparer.OrdinalIgnoreCase)
    {
        "no-referrer", "origin", "origin-when-cross-origin", "same-origin", "strict-origin",
        "strict-origin-when-cross-origin", "unsafe-url", "no-referrer-when-downgrade",
    };

    // Look and size only: nothing that can move the frame over the rest of the page. That leaves out position,
    // top, inset, z-index and transform, and also margin and vertical-align, where a negative length shifts the
    // frame (margin:-100vh 0 0 puts a full-page frame over the controls above it).
    private static readonly HashSet<string> StyleProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "border", "border-radius", "border-width", "border-style", "border-color", "width", "height",
        "max-width", "max-height", "min-width", "min-height", "aspect-ratio", "display", "overflow",
    };

    /// <summary>True when the html is exactly one safe iframe.</summary>
    /// <param name="html">The html of the embed.</param>
    /// <param name="siteHost">
    /// The host name of the site the html is written to. An iframe of that host is refused: it is not isolated from
    /// the page, so a script in what it loads (an SVG uploaded to the site, say) could reach the page. Null when it
    /// is not known, and nothing is then refused for its host.
    /// </param>
    public static bool IsSafeIframe(string? html, string? siteHost = null)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        Match tag = Iframe().Match(html);

        if (!tag.Success)
        {
            return false;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match attribute in Attributes().Matches(tag.Groups["attributes"].Value))
        {
            var name = attribute.Groups["name"].Value;

            // A repeated attribute is read as the first by a browser and as the last by other parsers.
            if (!seen.Add(name))
            {
                return false;
            }

            string? value = attribute.Groups["dq"].Success ? attribute.Groups["dq"].Value
                : attribute.Groups["sq"].Success ? attribute.Groups["sq"].Value
                : attribute.Groups["uq"].Success ? attribute.Groups["uq"].Value
                : null;

            if (!IsAllowed(name.ToLowerInvariant(), value, siteHost))
            {
                return false;
            }
        }

        // Without a source there is nothing to show.
        return seen.Contains("src");
    }

    private static bool IsAllowed(string name, string? value, string? siteHost)
    {
        // The address has its own rule for &, below: providers write a bare & in a query (SoundCloud does).
        if (value is not null && (OEmbedUrlSafety.HasControlCharacters(value) || (name != "src" && Entity().Replace(value, string.Empty).Contains('&'))))
        {
            return false;
        }

        return name switch
        {
            "src" => value is not null && IsSafeSource(value, siteHost),
            "width" or "height" => value is not null && Size().IsMatch(value),
            "frameborder" => IsOneOf(value, "0", "1", "no", "yes"),
            "scrolling" => IsOneOf(value, "yes", "no", "auto"),
            "loading" => IsOneOf(value, "lazy", "eager"),
            "referrerpolicy" => value is not null && ReferrerPolicies.Contains(value),
            "allowfullscreen" => value is null or "" || IsOneOf(value, "allowfullscreen", "true"),
            "title" => value is not null,
            "allow" => value is not null && Permissions().IsMatch(value),
            "sandbox" => value is not null && SandboxTokens().IsMatch(value),
            "data-testid" => value is not null && TestId().IsMatch(value),
            "style" => value is not null && IsSafeStyle(value),
            _ => false,
        };
    }

    private static bool IsOneOf(string? value, params string[] allowed)
        => value is not null && allowed.Any(option => option.Equals(value, StringComparison.OrdinalIgnoreCase));

    private static bool IsSafeSource(string value, string? siteHost)
    {
        if (value != value.Trim())
        {
            return false;
        }

        // A & in an address is either &amp; or, in the query, the start of a parameter (&name=). A browser reads
        // the second as it is, never as an entity, because a = follows the name. Any other & could be read as
        // an entity that changes the address, so it fails.
        var query = value.IndexOf('?');

        for (var i = value.IndexOf('&'); i >= 0; i = value.IndexOf('&', i + 1))
        {
            if (value.AsSpan(i).StartsWith("&amp;", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (query < 0 || i < query || !Parameter().IsMatch(value.AsSpan(i + 1)))
            {
                return false;
            }
        }

        var decoded = value.Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);

        // The address is written as scheme://host and nothing odd: not https:/media/x or https:media/x, which a
        // browser reads as a path on the site itself, and not backslashes, a third slash or a user name before the
        // host, which a browser and a parser can read as different hosts. The shape is checked on the text, so the
        // result does not depend on how a parser reads them.
        if (!SourceShape().IsMatch(decoded))
        {
            return false;
        }

        // The address must be an absolute http(s) URL, the same rule that is used for every other URL the
        // package passes on.
        return Uri.TryCreate(decoded, UriKind.Absolute, out Uri? uri)
            && OEmbedUrlSafety.IsSafeEnteredUrl(uri)

            // A frame of the site's own host is not isolated from the page. Any scheme or port of it is refused,
            // not only the exact origin, and the host is compared as the browser reads it (see Canonical). A
            // different address that redirects here cannot be seen from the text: for that, set a frame-src
            // Content-Security-Policy (see the README).
            && (siteHost is null || Canonical(uri.IdnHost) != Canonical(siteHost));
    }

    // One spelling of a host: lower case, in ASCII (punycode) as the browser sends it, without a trailing dot and
    // without the brackets of an IPv6 address. The Unicode and the punycode form of a name are the same host.
    private static string Canonical(string host)
    {
        var trimmed = host.Trim().TrimStart('[').TrimEnd(']').TrimEnd('.');

        // An IP address has one spelling too: [::1] and [0:0:0:0:0:0:0:1] are the same host.
        if (IPAddress.TryParse(trimmed, out IPAddress? address))
        {
            return address.ToString();
        }

        try
        {
            return new IdnMapping().GetAscii(trimmed).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return trimmed.ToLowerInvariant();
        }
    }

    private static bool IsSafeStyle(string value)
    {
        foreach (var declaration in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = declaration.IndexOf(':');

            if (colon <= 0)
            {
                return false;
            }

            if (!StyleProperties.Contains(declaration[..colon].Trim()) || !StyleValue().IsMatch(declaration[(colon + 1)..].Trim()))
            {
                return false;
            }
        }

        return true;
    }
}
