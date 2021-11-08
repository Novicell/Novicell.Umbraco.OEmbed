using System.Linq;
using Novicell.Umbraco.OEmbed.Core.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace Novicell.Umbraco.OEmbed.Core.Composing
{
    public class OEmbedComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // if the IOEmbedService Service is registered then we assume this has been added before so we don't do it again. 
            // - borrowed from Kevin Jump - thx ;)
            var registration = builder.Services
                .FirstOrDefault(x => x.ServiceType.Implements<IOEmbedService>());
            if (registration != null)
            {
                return;
            }

            builder.AddNovicellOEmbed();
        }
    }
}