namespace Novicell.Umbraco.OEmbed.Models;

/// <summary>
/// OEmbed Consumer Request.
/// </summary>
public record OEmbedRequest
{
    /// <summary>
    /// The URL to retrieve embedding information for.
    /// </summary>
    public required Uri Url { get; init; }

    /// <summary>
    /// The maximum width of the embedded resource. Only applies to some resource types (as specified below). For supported resource types, this parameter must be respected by providers.
    /// </summary>
    public int? MaxWidth { get; set; }

    /// <summary>
    /// The maximum height of the embedded resource. Only applies to some resource types (as specified below). For supported resource types, this parameter must be respected by providers.
    /// </summary>
    public int? MaxHeight { get; set; }
}
