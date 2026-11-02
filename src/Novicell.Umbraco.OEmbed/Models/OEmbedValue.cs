namespace Novicell.Umbraco.OEmbed.Models;

/// <summary>
/// One embed of an OEmbed picker property, as a Razor view gets it.
/// </summary>
/// <remarks>
/// Like Umbraco's <c>MediaWithCrops</c> for the media picker and <c>Link</c> for the URL picker: the
/// picked thing, with the URL the editor entered. <see cref="PropertyEditors.OEmbedPickerPropertyValueConverter"/>
/// returns one of these when the data type does not allow multiple embeds, and a collection of them when it
/// does. It is read from the stored value, which holds the same two parts, see <c>OEmbedDto</c>.
/// </remarks>
public record OEmbedValue
{
    /// <summary>
    /// The URL the editor entered. This is not <see cref="OEmbedResponse.Url"/>, which is the URL of the
    /// resource itself (the image of a photo).
    /// </summary>
    public required Uri Url { get; init; }

    /// <summary>
    /// What the provider returned for <see cref="Url"/>.
    /// </summary>
    public required OEmbedResponse OEmbed { get; init; }
}
