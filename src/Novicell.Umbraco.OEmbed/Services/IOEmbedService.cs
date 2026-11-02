using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core;
using OEmbedResponse = Novicell.Umbraco.OEmbed.Models.OEmbedResponse;

namespace Novicell.Umbraco.OEmbed.Services;

public interface IOEmbedService
{
    Task<Attempt<OEmbedResponse?, OEmbedOperationStatus>> GetOEmbedAsync(
        OEmbedRequest request,
        CancellationToken cancellationToken);
}
