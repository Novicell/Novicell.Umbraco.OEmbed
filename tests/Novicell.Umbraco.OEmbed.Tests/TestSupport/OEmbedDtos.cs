using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.Tests.TestSupport;

/// <summary>
/// Turns test embeds into what is stored (and what the save validators receive): the URL the editor
/// entered and the response with the provider's names.
/// </summary>
internal static class OEmbedDtos
{
    public static List<OEmbedDto> Of(IEnumerable<OEmbedValue?> values)
        => [.. values.Select(Of)];

    public static OEmbedDto Of(OEmbedValue? value)
        => value is null
            ? null!
            : new OEmbedDto { Url = value.Url?.OriginalString, OEmbed = value.OEmbed };
}
