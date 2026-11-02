using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.Services;

namespace Novicell.Umbraco.OEmbed.Configuration;

internal static class OEmbedHttpClientExtensions
{
    /// <summary>
    /// Registers the <see cref="HttpClient"/> used for all oEmbed traffic with a timeout, a redirect
    /// limit and the Umbraco User-Agent. The response size limit is applied when the body is read,
    /// see <see cref="OEmbedHttp"/>.
    /// </summary>
    internal static IServiceCollection AddOEmbedHttpClient(this IServiceCollection services)
    {
        services
            .AddHttpClient(OEmbedHttp.ClientName, client =>
            {
                // Covers the wait for the response headers; the body is bounded by OEmbedHttp.CreateDeadline.
            client.Timeout = OEmbedHttp.Timeout;
                _ = client.DefaultRequestHeaders.UserAgent.TryParseAdd(
                    global::Umbraco.Cms.Core.Constants.HttpClients.Headers.UserAgentProductName);
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider => CreateHandler(
                serviceProvider.GetRequiredService<IOptions<OEmbedOptions>>().Value.AllowPrivateNetworks));

        return services;
    }

    /// <summary>
    /// The handler behind the oEmbed client. Unless private networks are allowed it only connects to
    /// public addresses, and it does not use a proxy: with a proxy the connect check would see the
    /// proxy's address instead of the target's, so any proxy would pass it and forward the request
    /// anywhere.
    /// </summary>
    internal static SocketsHttpHandler CreateHandler(bool allowPrivateNetworks)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = OEmbedHttp.MaxRedirects,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        };

        if (!allowPrivateNetworks)
        {
            handler.UseProxy = false;
            handler.ConnectCallback = PublicNetworkConnector.ConnectAsync;
        }

        return handler;
    }
}
