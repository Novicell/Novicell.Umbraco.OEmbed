namespace Novicell.Umbraco.OEmbed.Models;

/// <summary>
/// Helpers for <see cref="OEmbedType"/>.
/// </summary>
internal static class OEmbedTypeExtensions
{
    /// <summary>
    /// Converts the enum value to its lowercase oEmbed spec / API string (matching
    /// the <see cref="OEmbedType"/> members' <c>JsonStringEnumMemberName</c> values).
    /// </summary>
    public static string ToApiValue(this OEmbedType type) => type switch
    {
        OEmbedType.Video => "video",
        OEmbedType.Photo => "photo",
        OEmbedType.Link => "link",
        OEmbedType.Rich => "rich",
        _ => type.ToString().ToLowerInvariant(),
    };
}
