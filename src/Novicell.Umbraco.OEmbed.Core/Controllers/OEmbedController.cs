using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel;
using System.Threading;
using Novicell.Umbraco.OEmbed.Core.PropertyEditors;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Novicell.Umbraco.OEmbed.Core.Services;

namespace Novicell.Umbraco.OEmbed.Core.Controllers
{
    /// <summary>
    /// A controller used for the embed dialog
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [PluginController(OEmbedPropertyEditor.PluginAreaName), IsBackOffice]
    [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
    public class OEmbedController : UmbracoAuthorizedJsonController
    {
        private readonly ILocalizedTextService _localizedTextService;
        private readonly IOEmbedService _oembedService;

        public OEmbedController(
            ILocalizedTextService localizedTextService,
            IOEmbedService oembedService)
        {
            _localizedTextService = localizedTextService;
            _oembedService = oembedService;
        }

        public async Task<IActionResult> Get(string url, int maxwidth = 0, int maxheight = 0, string type = null)
        {
            Uri _url = null;

            if (string.IsNullOrWhiteSpace(url))
            {
                var errorMessage = _localizedTextService.Localize("novicellOEmbed", "url_missing");
                ModelState.AddModelError(nameof(url), errorMessage);
            }
            else if (!Uri.TryCreate(url, UriKind.Absolute, out _url))
            {
                var errorMessage = _localizedTextService.Localize("novicellOEmbed", "url_invalid");
                ModelState.AddModelError(nameof(url), errorMessage);
            }

            if (maxwidth < 0)
            {
                ModelState.AddModelError(nameof(maxwidth), "Must be higher than 0");
            }

            if (maxheight < 0)
            {
                ModelState.AddModelError(nameof(maxheight), "Must be higher than 0");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(modelStateDictionary: ModelState);
            }

            var embed = await _oembedService.GetOEmbedAsync(_url, maxwidth, maxheight);

            if (embed.Success)
            {
                var result = embed.Result;

                if (!string.IsNullOrWhiteSpace(type) && !type.InvariantEquals(result?.Type))
                {
                    var tokens = new Dictionary<string, string> { { nameof(type), type } };
                    var errorMessage = _localizedTextService.Localize("novicellOEmbed", "embed_type_invalid", Thread.CurrentThread.CurrentUICulture, tokens);
                    ModelState.AddModelError(nameof(url), errorMessage);
                }
            }
            else if (embed.Exception is OEmbedUrlNotSupportedException)
            {
                var errorMessage = _localizedTextService.Localize("novicellOEmbed", "url_not_supported");
                ModelState.AddModelError(nameof(url), errorMessage);
            }
            else if (embed.Exception is OEmbedException e)
            {
                ModelState.AddModelError(nameof(url), e.Message);
            }
            else if (embed.Result is null)
            {
                var errorMessage = _localizedTextService.Localize("novicellOEmbed", "embed_not_found");
                ModelState.AddModelError(nameof(url), errorMessage);
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(modelStateDictionary: ModelState);
            }

            return Ok(embed.Result);
        }
    }

}