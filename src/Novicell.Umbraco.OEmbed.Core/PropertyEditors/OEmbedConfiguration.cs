using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.Core.PropertyEditors
{
    public class OEmbedConfiguration
    {
        [ConfigurationField("type", "Type", "radiobuttonlist", Description = "Allowed Type")]
        public string Type { get; set; }
    }
}