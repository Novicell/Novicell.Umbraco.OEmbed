using System.Text.Json.Serialization;
using Novicell.Umbraco.OEmbed.Models;

namespace Novicell.Umbraco.OEmbed.DeliveryApi;

/// <summary>
/// One embed of an OEmbed picker property, as the Delivery API returns it.
/// </summary>
/// <remarks>
/// This is the contract for headless sites, and it is deliberately separate from the model used in
/// Razor (<see cref="OEmbedValue"/>), so that renaming or reshaping that model cannot change
/// what a site receives by accident. The property names are written out for the same reason, and are
/// camelCase like the rest of the Delivery API. A property with several embeds is an array of these.
/// The <see cref="ApiOEmbedResponse.Html"/> is what the provider returned when the editor picked the
/// URL, and is not verified, so a site should treat it as editor-supplied content.
/// </remarks>
/// <param name="Url">The URL the editor entered.</param>
/// <param name="OEmbed">The oEmbed response for <paramref name="Url"/>.</param>
internal sealed record ApiOEmbedValue(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("oembed")] ApiOEmbedResponse OEmbed)
{
    internal static ApiOEmbedValue From(OEmbedValue value)
        => new(value.Url.OriginalString, ApiOEmbedResponse.From(value.OEmbed));
}
