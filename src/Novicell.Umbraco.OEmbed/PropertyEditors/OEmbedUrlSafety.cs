using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.Services;

namespace Novicell.Umbraco.OEmbed.PropertyEditors;

/// <summary>
/// The rule for which URLs in a stored OEmbed value are safe to hand on, shared by the save validator
/// (<see cref="OEmbedPickerPropertyValueEditor.UrlValidator"/>) and the published value converter.
/// </summary>
/// <remarks>
/// Both are needed. The validator gives the editor an early error, but it is not the only way a value
/// reaches the database: uSync, a direct database change or an older version write it without
/// validation, and the validator parses the JSON with Umbraco's serializer while the converter parses
/// it case-insensitively, so they can read different URLs from the same JSON. The converter therefore
/// applies the rule to what it actually produces, so a site is never handed a script-capable URL.
/// </remarks>
internal static class OEmbedUrlSafety
{
    /// <summary>
    /// True for the URL the editor entered when it is an absolute http(s) URL, as that is all the
    /// server ever resolves, and it has no control characters.
    /// </summary>
    internal static bool IsSafeEnteredUrl(Uri? url)
        => url is not null
           && !HasControlCharacters(url.OriginalString)
           && url.IsAbsoluteUri
           && OEmbedHttp.IsHttpUrl(url);

    /// <summary>
    /// True for a URL inside the stored response (resource, author, provider, thumbnail): none, an
    /// absolute http(s) URL, or a relative one (some providers return those), but never an absolute URL
    /// with another scheme and never one with a control character in it.
    /// </summary>
    internal static bool IsSafeResponseUrl(Uri? uri)
        => uri is null
           || (!HasControlCharacters(uri.OriginalString)
               && (!uri.IsAbsoluteUri || OEmbedHttp.IsHttpUrl(uri)));

    /// <summary>
    /// True if the text contains a control character (U+0000-U+001F, U+007F-U+009F). System.Uri accepts
    /// <c>java\nscript:alert(1)</c> as a relative URI, but browsers drop tabs and newlines from a URL
    /// and ignore leading control characters, so it becomes <c>javascript:</c> when rendered as a link.
    /// No legitimate URL has a raw control character in it (they are percent-encoded).
    /// </summary>
    internal static bool HasControlCharacters(string? text)
        => text is not null && text.Any(char.IsControl);

    /// <summary>True if the item, its response and every URL in it are safe.</summary>
    internal static bool IsSafe(OEmbedValue? item)
        => item?.OEmbed is not null
           && IsSafeEnteredUrl(item.Url)
           && IsSafeResponseUrl(item.OEmbed.Url)
           && IsSafeResponseUrl(item.OEmbed.AuthorUrl)
           && IsSafeResponseUrl(item.OEmbed.ProviderUrl)
           && IsSafeResponseUrl(item.OEmbed.ThumbnailUrl);

    /// <summary>
    /// The item with every unsafe URL in its response removed, or null if the item itself is not usable
    /// (it is null, has no response, or the URL the editor entered is not safe).
    /// </summary>
    internal static OEmbedValue? Sanitize(OEmbedValue? item)
    {
        if (item?.OEmbed is null || !IsSafeEnteredUrl(item.Url))
        {
            return null;
        }

        OEmbedResponse response = item.OEmbed;

        return item with
        {
            OEmbed = response with
            {
                Url = SafeOrNull(response.Url),
                AuthorUrl = SafeOrNull(response.AuthorUrl),
                ProviderUrl = SafeOrNull(response.ProviderUrl),
                ThumbnailUrl = SafeOrNull(response.ThumbnailUrl),
            },
        };
    }

    private static Uri? SafeOrNull(Uri? uri) => IsSafeResponseUrl(uri) ? uri : null;
}
