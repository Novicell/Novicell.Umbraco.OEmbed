using System.Net.Mime;

namespace Novicell.Umbraco.OEmbed.Services;

/// <summary>
/// The media types and link relations oEmbed uses. Only the JSON format is supported: the request asks for
/// it (<c>format=json</c>), and an endpoint that is advertised for XML only is not used.
/// </summary>
internal static class OEmbedMediaTypes
{
    /// <summary>The media type of an oEmbed JSON response, and of an oEmbed JSON endpoint advertised in a link.</summary>
    internal const string Json = MediaTypeNames.Application.Json;

    internal const string JsonOEmbed = Json + "+oembed";

    internal static bool IsJson(string? mediaType) => Is(mediaType, Json);

    internal static bool IsJsonOEmbed(string? mediaType) => Is(mediaType, JsonOEmbed);

    /// <summary>True for the link relations <c>alternate</c> and <c>alternative</c>, which pages use for the oEmbed link.</summary>
    internal static bool IsAlternateOrAlternative(string? rel)
        => Is(rel, "alternate") || Is(rel, "alternative");

    private static bool Is(string? value, string expected)
        => !string.IsNullOrWhiteSpace(value) && expected.Equals(value, StringComparison.InvariantCultureIgnoreCase);
}
