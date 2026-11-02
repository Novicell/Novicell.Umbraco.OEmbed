using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Novicell.Umbraco.OEmbed;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.Services;

namespace Novicell.Umbraco.OEmbed.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = Constants.GroupName)]
public class OEmbedApiController(IOEmbedService oembedService)
    : OEmbedApiControllerBase
{
    // Passed on to the provider as maxwidth/maxheight; no embed needs more than this.
    private const int MaxDimension = 4096;

    [HttpGet("oembed")]
    [ProducesResponseType<OEmbedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOEmbedAsync(
        Uri url,
        [Range(1, MaxDimension)] int? maxwidth,
        [Range(1, MaxDimension)] int? maxheight,
        CancellationToken cancellationToken)
    {
        var request = new OEmbedRequest
        {
            Url = url,
            MaxWidth = maxwidth,
            MaxHeight = maxheight
        };

        global::Umbraco.Cms.Core.Attempt<OEmbedResponse?, OEmbedOperationStatus> response = await oembedService
            .GetOEmbedAsync(request, cancellationToken);

        return response.Success
            ? Ok(response.Result)
            : OEmbedOperationStatusResult(response.Status);
    }
}
