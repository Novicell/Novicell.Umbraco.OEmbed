using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.PropertyEditors
{
    public class OEmbedConfiguration
    {
        [ConfigurationField("type")]
        public string Type { get; set; }
    }
}