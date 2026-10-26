using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.Modules.Shared;
using Novicell.Umbraco.OEmbed.Services;
using Umbraco.Cms.Web.Common.Routing;

namespace Novicell.Umbraco.OEmbed.Modules.PropertyEditors.OEmbed;

[ApiExplorerSettings(GroupName = "OEmbed")]
[BackOfficeRoute("oembed")]
public sealed class OEmbedController : OEmbedApiControllerBase
{
    private readonly IOEmbedService _oEmbedService;

    public OEmbedController(IOEmbedService oEmbedService)
    {
        _oEmbedService = oEmbedService;
    }

    [HttpGet("get")]
    [ProducesResponseType<OEmbedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromQuery] string url, [FromQuery] int maxWidth = 800,
        [FromQuery] int maxHeight = 600)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest("url is required");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
        {
            return BadRequest("url is not a valid absolute URI");
        }

        OEmbedResponse response = null;
        global::Umbraco.Cms.Core.Attempt<OEmbedResponse> attempt =
            await _oEmbedService.GetOEmbedAsync(uri, maxWidth, maxHeight);

        if (attempt.Success)
        {
            response = attempt.Result;
        }

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }
}