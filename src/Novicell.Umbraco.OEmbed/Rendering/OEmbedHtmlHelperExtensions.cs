using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.Rendering;

// In Umbraco's own namespace for helper extensions, which a view imports by default.
namespace Umbraco.Extensions;

/// <summary>
/// Writes an embed to a Razor view.
/// </summary>
public static class OEmbedHtmlHelperExtensions
{
    /// <summary>
    /// Writes the html the provider returned for an embed, exactly as it is, when it is safe to; otherwise nothing.
    /// </summary>
    /// <remarks>
    /// The html of a saved embed is posted by the backoffice and is not verified, so it is not written blindly.
    /// By default only a single <c>&lt;iframe&gt;</c> with ordinary attributes passes (YouTube, Vimeo, Spotify,
    /// SoundCloud and the like), and not one of the site's own host; markup with a script of its own is left out.
    /// Nothing is added or changed: the
    /// result is the provider's html or empty. A photo or link embed has no html, so write its
    /// <see cref="OEmbedResponse.Url"/> and <see cref="OEmbedResponse.Title"/> yourself.
    /// Set <see cref="OEmbedRenderOptions.Scripts"/> to <see cref="OEmbedScripts.Raw"/> to write any html as it is.
    /// </remarks>
    /// <param name="htmlHelper">The helper of the view.</param>
    /// <param name="embed">The embed, as the property gives it.</param>
    /// <param name="options">Optional settings.</param>
    /// <returns>The provider's html, or empty content.</returns>
    public static IHtmlContent OEmbed(this IHtmlHelper htmlHelper, OEmbedValue embed, OEmbedRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(embed);

        var html = embed.OEmbed.Html;

        if (string.IsNullOrWhiteSpace(html))
        {
            return HtmlString.Empty;
        }

        // The host of the request is the site's own host: an iframe of it is not isolated from the page.
        var siteHost = htmlHelper.ViewContext?.HttpContext?.Request.Host.Host;

        return options?.Scripts == OEmbedScripts.Raw || OEmbedHtmlGate.IsSafeIframe(html, siteHost)
            ? new HtmlString(html)
            : HtmlString.Empty;
    }
}
