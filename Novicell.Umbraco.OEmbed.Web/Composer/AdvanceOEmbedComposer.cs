using Novicell.OEmbed.Web.Manifest;
using Umbraco.Cms.Core.Composing;

namespace Novicell.OEmbed.Web.Composer
{
    public class AdvanceOEmbedComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.ManifestFilters().Append<AdvanceOEmbedManifestFilter>();
        }
    }
}
