using System;
using System.Threading.Tasks;
using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core;

namespace Novicell.Umbraco.OEmbed.Services
{
    public interface IOEmbedService
    {
        Task<Attempt<OEmbedResponse>> GetOEmbedAsync(Uri url, int maxwidth, int maxheight);
    }
}