using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Serialization;

namespace Novicell.Umbraco.OEmbed.EmbedProviders;

/// <summary>
///     Embed Provider for Spotify, the audio streaming and media services platform.
/// </summary>
public class Spotify : OEmbedProviderBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Spotify"/> class.
    /// </summary>
    /// <param name="jsonSerializer">The serializer used to read the provider's response.</param>
    public Spotify(IJsonSerializer jsonSerializer)
        : base(jsonSerializer)
    {
    }

    /// <inheritdoc/>
    public override string ApiEndpoint => "https://open.spotify.com/oembed";

    /// <inheritdoc/>
    public override string[] UrlSchemeRegex =>
    [
        @"^https?:\/\/open\.spotify\.com\/",
        @"^https?:\/\/spotify\.link\/",
    ];

    /// <inheritdoc/>
    public override Dictionary<string, string> RequestParams => [];

    /// <inheritdoc/>
    public override async Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
        => await GetJsonBasedMarkupAsync(url, maxWidth, maxHeight, cancellationToken);
}
