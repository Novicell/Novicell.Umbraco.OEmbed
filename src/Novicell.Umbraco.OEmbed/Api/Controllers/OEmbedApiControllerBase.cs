using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Novicell.Umbraco.OEmbed;
using Novicell.Umbraco.OEmbed.Services;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

namespace Novicell.Umbraco.OEmbed.Api.Controllers
{
    [ApiController]
    [BackOfficeRoute(Constants.ApiName + "/api/v{version:apiVersion}")]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
    [MapToApi(Constants.ApiName)]
    public abstract class OEmbedApiControllerBase : ManagementApiControllerBase
    {
        protected IActionResult OEmbedOperationStatusResult(OEmbedOperationStatus status)
            => OperationStatusResult(status, problemDetailsBuilder => status switch
            {
                OEmbedOperationStatus.InvalidResponse => BadRequest(problemDetailsBuilder
                    .WithTitle("Invalid response from provider.")
                    .WithDetail("The oEmbed provider was not able to handle the request, please ensure the url is correct.")
                    .Build()),
                OEmbedOperationStatus.EmbedNotFound => BadRequest(problemDetailsBuilder
                    .WithTitle("Not found at provider.")
                    .WithDetail("The specified url was not found at the oEmbed provider.")
                    .Build()),
                OEmbedOperationStatus.ProviderNotSupported => BadRequest(problemDetailsBuilder
                    .WithTitle("The specified url is not supported.")
                    .WithDetail("No oEmbed provider was found for the specified url.")
                    .Build()),
                _ => StatusCode(StatusCodes.Status500InternalServerError, problemDetailsBuilder
                    .WithTitle("Unknown oEmbed operation status.")
                    .Build()),
            });
    }
}
