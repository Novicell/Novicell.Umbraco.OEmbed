using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novicell.Umbraco.OEmbed.Models;

/// <summary>
/// OEmbed Provider Response.
/// </summary>
/// <remarks>
/// What a provider returned, as read by <see cref="Services.IOEmbedService"/>, with the names the provider
/// used (those of the oEmbed specification). The same shape is what the Management API returns to the
/// backoffice, what the backoffice stores, and what is read from the stored value.
/// A field of an unexpected kind (a width sent as text is read as a number, anything else is null) never
/// makes the response unreadable, and a field the provider returned that has no property here is kept in
/// <see cref="Extra"/>. A Razor view gets it as <see cref="OEmbedValue.OEmbed"/>.
/// </remarks>
public record OEmbedResponse
{
    /// <summary>
    /// The resource type. Valid values, along with value-specific parameters, are described below.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(OEmbedTypeConverter))]
    public required OEmbedType Type { get; init; }

    /// <summary>
    /// The oEmbed version number. This must be 1.0.
    /// </summary>
    /// <remarks>
    /// Optional rather than required: some providers leave it out, although the specification requires it.
    /// </remarks>
    [JsonPropertyName("version")]
    [JsonConverter(typeof(LenientVersionConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    /// <summary>
    /// A text title, describing the resource.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonConverter(typeof(LenientStringConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// The name of the author/owner of the resource.
    /// </summary>
    [JsonPropertyName("author_name")]
    [JsonConverter(typeof(LenientStringConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AuthorName { get; set; }

    /// <summary>
    /// A URL for the author/owner of the resource.
    /// </summary>
    [JsonPropertyName("author_url")]
    [JsonConverter(typeof(LenientUriConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Uri? AuthorUrl { get; set; }

    /// <summary>
    /// The name of the resource provider.
    /// </summary>
    [JsonPropertyName("provider_name")]
    [JsonConverter(typeof(LenientStringConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProviderName { get; set; }

    /// <summary>
    /// The url of the resource provider.
    /// </summary>
    [JsonPropertyName("provider_url")]
    [JsonConverter(typeof(LenientUriConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Uri? ProviderUrl { get; set; }

    /// <summary>
    /// A URL to a thumbnail image representing the resource.
    /// </summary>
    /// <remarks>
    /// The thumbnail must respect any maxwidth and maxheight parameters. If this parameter is present, <see cref="ThumbnailWidth"/> and <see cref="ThumbnailHeight"/> must also be present.
    /// </remarks>
    [JsonPropertyName("thumbnail_url")]
    [JsonConverter(typeof(LenientUriConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Uri? ThumbnailUrl { get; set; }

    /// <summary>
    /// The height of the optional thumbnail.
    /// </summary>
    /// <remarks>
    /// If this parameter is present, <see cref="ThumbnailUrl" /> and <see cref="ThumbnailWidth"/> must also be present.
    /// </remarks>
    [JsonPropertyName("thumbnail_height")]
    [JsonConverter(typeof(LenientIntConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ThumbnailHeight { get; set; }

    /// <summary>
    /// The width of the optional thumbnail.
    /// </summary>
    /// <remarks>
    /// If this parameter is present, <see cref="ThumbnailUrl" /> and <see cref="ThumbnailHeight"/> must also be present.
    /// </remarks>
    [JsonPropertyName("thumbnail_width")]
    [JsonConverter(typeof(LenientIntConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ThumbnailWidth { get; set; }

    /// <summary>
    /// The HTML required to embed a video player.
    /// </summary>
    /// <remarks>The HTML should have no padding or margins. Consumers may wish to load the HTML in an off-domain iframe to avoid XSS vulnerabilities.</remarks>
    [JsonPropertyName("html")]
    [JsonConverter(typeof(LenientStringConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Html { get; set; }

    /// <summary>
    /// The source URL of the image.
    /// </summary>
    /// <remarks>Consumers should be able to insert this URL into an &lt;img&gt; element. Only HTTP and HTTPS URLs are valid.</remarks>
    [JsonPropertyName("url")]
    [JsonConverter(typeof(LenientUriConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Uri? Url { get; set; }

    /// <summary>
    /// The height in pixels of the image specified in the <see cref="Url" /> parameter.
    /// </summary>
    /// <remarks>When <see cref="Type"/> is <see cref="OEmbedType.Video"/> or <see cref="OEmbedType.Rich"/>, this is height in pixels required to display the HTML.</remarks>
    [JsonPropertyName("height")]
    [JsonConverter(typeof(LenientIntConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Height { get; set; }

    /// <summary>
    /// The width in pixels of the image specified in the <see cref="Url" /> parameter.
    /// </summary>
    /// <remarks>When <see cref="Type"/> is <see cref="OEmbedType.Video"/> or <see cref="OEmbedType.Rich"/>, this is width in pixels required to display the HTML.</remarks>
    [JsonPropertyName("width")]
    [JsonConverter(typeof(LenientIntConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Width { get; set; }

    /// <summary>
    /// The suggested cache lifetime for this resource, in seconds. Consumers may choose to use this value or not.
    /// </summary>
    [JsonPropertyName("cache_age")]
    [JsonConverter(typeof(LenientIntConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CacheAge { get; set; }

    /// <summary>
    /// Everything the provider returned that has no property on this type (provider specific fields), with
    /// the names the provider used. It is passed on to the backoffice and stored with the rest, and read
    /// from the stored value with <see cref="TryGetValue{T}"/>. These values are exactly what the provider
    /// sent: unlike the URLs above they are not checked, so treat them as editor-supplied.
    /// </summary>
    [JsonExtensionData]
    [JsonInclude]
    internal Dictionary<string, JsonElement>? Extra { get; init; }

    /// <summary>
    /// Reads a field the provider returned that has no property on this type, for example
    /// <c>TryGetValue("thumbnail_url_with_play_button", out string? url)</c>.
    /// </summary>
    /// <param name="name">The name the provider used.</param>
    /// <param name="value">The value, when it is there and can be read as <typeparamref name="T"/>.</param>
    /// <returns>True when the field is there and can be read as <typeparamref name="T"/>.</returns>
    public bool TryGetValue<T>(string name, out T? value)
    {
        value = default;

        if (Extra is null || !Extra.TryGetValue(name, out JsonElement element))
        {
            return false;
        }

        try
        {
            value = element.Deserialize<T>();
            return value is not null;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return false;
        }
    }
}
