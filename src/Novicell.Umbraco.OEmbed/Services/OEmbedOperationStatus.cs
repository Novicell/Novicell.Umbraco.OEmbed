namespace Novicell.Umbraco.OEmbed.Services;

/// <summary>
/// Represents the status of an oEmbed operation.
/// </summary>
public enum OEmbedOperationStatus
{
    /// <summary>
    /// The OEmbed operation was successful.
    /// </summary>
    Success,
    /// <summary>
    ///  
    /// </summary>
    ProviderNotSupported,
    /// <summary>
    ///
    /// </summary>
    EmbedNotFound,
    /// <summary>
    /// The OEmbed request failed.
    /// </summary>
    InvalidResponse
}
