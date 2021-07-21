using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.PropertyEditors
{
    public class OEmbedConfiguration
    {
        [ConfigurationField("type", "Type", "radiobuttonlist", Description = "Allowed Type")]
        public string Type { get; set; }
    }
}