using Novicell.Umbraco.OEmbed.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Novicell.Umbraco.OEmbed.Composers;

/// <inheritdoc />
internal class OEmbedComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNovicellOEmbed();
    }
}
