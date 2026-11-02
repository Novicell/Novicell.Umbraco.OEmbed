using System.Text.Json;
using System.Text.Json.Serialization;
using Novicell.Umbraco.OEmbed.Models;

namespace Novicell.Umbraco.OEmbed.PropertyEditors;

/// <summary>
/// One embed as it is stored: the URL the editor entered, and the oEmbed response as the provider returned
/// it, with the provider's names and everything else it sent. The same role as Umbraco's
/// <c>MediaWithCropsDto</c> has for the media picker.
/// </summary>
/// <remarks>
/// This is what the save validators read, and what <see cref="OEmbedPickerPropertyValueConverter"/> reads before
/// it maps it to <see cref="OEmbedValue"/>, see <see cref="ToValue"/>. The response is
/// an <see cref="OEmbedResponse"/>, which reads a field of an unexpected kind as null and keeps the fields
/// it has no property for, so a provider's unusual value can never make a whole property unreadable, and
/// nothing the provider sent is lost.
/// A stored value is an array of these. A property saved before multi-value support, which is also what
/// the Umbraco 9 version stored, is one object; <see cref="Deserialize"/> reads both.
/// </remarks>
internal sealed record OEmbedDto
{
    /// <summary>The URL the editor entered.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>
    /// The provider's response. Null when there is none, or when it cannot be used (it has no valid type),
    /// so that the item is reported as unusable instead of the whole value failing to read.
    /// </summary>
    [JsonPropertyName("oembed")]
    [JsonConverter(typeof(TolerantObjectConverter<OEmbedResponse>))]
    public OEmbedResponse? OEmbed { get; init; }

    /// <summary>
    /// Reads a stored value: an array of items, or the single-object shape. An item of the wrong shape
    /// (for example a URL that is not text) is null in the result and does not take the others with it;
    /// a value that is not JSON gives an empty list.
    /// </summary>
    public static List<OEmbedDto?> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => [.. document.RootElement.EnumerateArray().Select(ReadItem)],
                JsonValueKind.Object => [ReadItem(document.RootElement)],
                _ => [],
            };
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// What a site is handed for a stored value: the picked embeds, without an item that cannot be used
    /// and without a script-capable URL, however the value got into the database. See
    /// <see cref="OEmbedUrlSafety"/>.
    /// </summary>
    public static List<OEmbedValue> ToValues(string? json)
        => Deserialize(json)
            .Select(dto => dto?.ToValue())
            .Select(OEmbedUrlSafety.Sanitize)
            .OfType<OEmbedValue>()
            .ToList();

    /// <summary>
    /// The picked embed, or null when the item cannot be used: it has no absolute URL, or no response with a valid
    /// type. URLs are not checked here, see <see cref="OEmbedUrlSafety"/>.
    /// </summary>
    public OEmbedValue? ToValue()
        => OEmbed is null || !Uri.TryCreate(Url, UriKind.Absolute, out Uri? url)
            ? null
            : new OEmbedValue { Url = url, OEmbed = OEmbed };

    private static OEmbedDto? ReadItem(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return item.Deserialize<OEmbedDto>();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
