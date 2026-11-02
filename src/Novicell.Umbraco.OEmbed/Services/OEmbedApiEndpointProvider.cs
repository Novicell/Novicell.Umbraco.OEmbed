using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;
using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Media;
using Umbraco.Cms.Core.Media.EmbedProviders;

namespace Novicell.Umbraco.OEmbed.Services;

internal class OEmbedApiEndpointProvider(EmbedProvidersCollection providers) : IOEmbedApiEndpointProvider
{
    public async Task<OEmbedApiEndpoint?> GetApiEndpointAsync(Uri url)
    {
        Attempt<IEmbedProvider> provider = FindEmbedProvider(url);

        if (provider.Result is null)
        {
            return null;
        }

        Uri endpoint = ToJsonEndpoint(BuildApiEndpointUrl(provider.Result));

        return new OEmbedApiEndpoint(provider.Result.GetType().Name, endpoint);
    }

    /// <summary>
    /// Only JSON is read, and some of Umbraco's providers point at an endpoint that is XML by its path
    /// (Vimeo's <c>oembed.xml</c>, TED's <c>oembed.xml</c>), where asking for <c>format=json</c> changes
    /// nothing. Those providers serve the same response at the same path ending in <c>.json</c>.
    /// </summary>
    internal static Uri ToJsonEndpoint(Uri endpoint)
        => endpoint.AbsolutePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
            ? new UriBuilder(endpoint) { Path = endpoint.AbsolutePath[..^".xml".Length] + ".json" }.Uri
            : endpoint;

    private Attempt<IEmbedProvider> FindEmbedProvider(Uri url)
    {
        var isUrlMatch = new Func<string, string[], bool>((input, patterns)
            => patterns.Any(p => Regex.IsMatch(input, p, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1))));

        IEmbedProvider? provider = providers?
            .Where(x => x.UrlSchemeRegex.Length > 0)
            .FirstOrDefault(x => isUrlMatch(url.OriginalString, x.UrlSchemeRegex));

        return Attempt<IEmbedProvider>.If(provider != null, provider);
    }

    private static Uri BuildApiEndpointUrl(IEmbedProvider provider)
        => BuildApiEndpointUrl(provider.ApiEndpoint, provider.RequestParams);

    private static Uri BuildApiEndpointUrl(string endpoint, IDictionary<string, string> parameters)
    {
        if (parameters.Count <= 0)
        {
            return new Uri(endpoint);
        }

        var builder = new UriBuilder(endpoint);

        NameValueCollection queryString = HttpUtility.ParseQueryString(builder.Query);

        foreach ((string? key, string? value) in parameters)
        {
            // Not encoded here: ToString() below encodes every value.
            queryString[key] = value;
        }

        builder.Query = queryString.ToString() ?? string.Empty;

        return builder.Uri;
    }
}
