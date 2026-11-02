using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Novicell.Umbraco.OEmbed.Models;

namespace Novicell.Umbraco.OEmbed.DeliveryApi;

/// <summary>
/// The oEmbed response of an <see cref="ApiOEmbedValue"/>, with the fields of the oEmbed specification.
/// Fields the provider did not return are null.
/// </summary>
/// <param name="Type">The resource type: <c>video</c>, <c>photo</c>, <c>link</c> or <c>rich</c>.</param>
/// <param name="Version">The oEmbed version, when the provider returned one.</param>
/// <param name="Title">A text title describing the resource.</param>
/// <param name="AuthorName">The name of the author or owner of the resource.</param>
/// <param name="AuthorUrl">A URL for the author or owner of the resource.</param>
/// <param name="ProviderName">The name of the resource provider.</param>
/// <param name="ProviderUrl">The URL of the resource provider.</param>
/// <param name="ThumbnailUrl">A URL to a thumbnail image representing the resource.</param>
/// <param name="ThumbnailHeight">The height of the thumbnail, in pixels.</param>
/// <param name="ThumbnailWidth">The width of the thumbnail, in pixels.</param>
/// <param name="Html">The HTML to embed, for the <c>video</c> and <c>rich</c> types.</param>
/// <param name="Url">The URL of the resource, for the <c>photo</c> type.</param>
/// <param name="Height">The height of the resource, in pixels.</param>
/// <param name="Width">The width of the resource, in pixels.</param>
/// <param name="CacheAge">How long, in seconds, the provider suggests the response is cached.</param>
/// <param name="Extra">
/// Everything else the provider returned, with the names the provider used (for example Vimeo's
/// <c>video_id</c>). Left out when the provider returned nothing more. These values are exactly what the
/// provider sent and are not checked, so a site should treat them as editor-supplied content.
/// </param>
internal sealed record ApiOEmbedResponse(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("authorName")] string? AuthorName,
    [property: JsonPropertyName("authorUrl")] string? AuthorUrl,
    [property: JsonPropertyName("providerName")] string? ProviderName,
    [property: JsonPropertyName("providerUrl")] string? ProviderUrl,
    [property: JsonPropertyName("thumbnailUrl")] string? ThumbnailUrl,
    [property: JsonPropertyName("thumbnailHeight")] int? ThumbnailHeight,
    [property: JsonPropertyName("thumbnailWidth")] int? ThumbnailWidth,
    [property: JsonPropertyName("html")] string? Html,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("cacheAge")] int? CacheAge,
    [property: JsonPropertyName("extra")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonObject? Extra)
{
    internal static ApiOEmbedResponse From(OEmbedResponse response)
        => new(
            response.Type.ToApiValue(),
            response.Version,
            response.Title,
            response.AuthorName,
            response.AuthorUrl?.OriginalString,
            response.ProviderName,
            response.ProviderUrl?.OriginalString,
            response.ThumbnailUrl?.OriginalString,
            response.ThumbnailHeight,
            response.ThumbnailWidth,
            response.Html,
            response.Url?.OriginalString,
            response.Height,
            response.Width,
            response.CacheAge,
            ExtraOf(response));

    // A JsonObject, because its names are written as they are whatever naming policy the serializer has.
    private static JsonObject? ExtraOf(OEmbedResponse response)
        => response.Extra is { Count: > 0 } extra
            ? new JsonObject(extra.Select(field => KeyValuePair.Create(field.Key, JsonNode.Parse(field.Value.GetRawText()))))
            : null;
}
