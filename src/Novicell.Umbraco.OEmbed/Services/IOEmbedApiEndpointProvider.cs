using Novicell.Umbraco.OEmbed.Models;

namespace Novicell.Umbraco.OEmbed.Services;

internal interface IOEmbedApiEndpointProvider
{
    Task<OEmbedApiEndpoint?> GetApiEndpointAsync(Uri url);
}
