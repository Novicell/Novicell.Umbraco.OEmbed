using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Serialization;

namespace Novicell.Umbraco.OEmbed.EmbedProviders;

/// <summary>
///     Embed Provider for Reddit, the network of communities-based discussion platform.
/// </summary>
public class Reddit : OEmbedProviderBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Reddit"/> class.
    /// </summary>
    /// <param name="jsonSerializer">The serializer used to read the provider's response.</param>
    public Reddit(IJsonSerializer jsonSerializer)
        : base(jsonSerializer)
    {
    }

    /// <inheritdoc/>
    public override string ApiEndpoint => "https://www.reddit.com/oembed";

    /// <inheritdoc/>
    public override string[] UrlSchemeRegex =>
    [
        @"^https?:\/\/(www\.)?reddit\.com\/r\/[^\/]+\/comments\/",
    ];

    /// <inheritdoc/>
    public override Dictionary<string, string> RequestParams => [];

    /// <inheritdoc/>
    public override async Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
        => await GetJsonBasedMarkupAsync(url, maxWidth, maxHeight, cancellationToken);
}
