using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core.WebAssets;
using Umbraco.Cms.Infrastructure.WebAssets;

namespace Novicell.Umbraco.OEmbed.PropertyEditors
{
    /// <inheritdoc />
    [PropertyEditorAsset(AssetType.Javascript, PluginFolder + "oembed.js")]
    [PropertyEditorAsset(AssetType.Css, PluginFolder + "oembed.css")]
    [DataEditor(PropertyEditorAlias, EditorType.PropertyValue, "Novicell OEmbed",
        PluginFolder + "oembed.propertyeditor.html", Group = "media", ValueType = ValueTypes.Json)]
    public class OEmbedPropertyEditor : DataEditor
    {
        private const string PluginFolder = "/App_Plugins/" + PropertyEditorAlias + "/";
        public const string PropertyEditorAlias = "Novicell.OEmbed";
        public const string PluginAreaName = "Novicell";

        protected override IConfigurationEditor CreateConfigurationEditor()
        {
            return new OEmbedConfigurationEditor(null);
        }

        public OEmbedPropertyEditor(IDataValueEditorFactory dataValueEditorFactory) : base(dataValueEditorFactory)
        {
            
        }
    }
}