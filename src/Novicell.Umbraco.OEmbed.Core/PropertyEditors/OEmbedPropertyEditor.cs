using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.WebAssets;

namespace Novicell.Umbraco.OEmbed.Core.PropertyEditors
{
    /// <inheritdoc />
    [DataEditor(PropertyEditorAlias, "Novicell OEmbed",
        PluginFolder + "oembed.propertyeditor.html", Group = "media", ValueType = ValueTypes.Json)]
    internal class OEmbedPropertyEditor : DataEditor
    {
        public class OEmbedJavaScriptFile : JavaScriptFile
        {
            public OEmbedJavaScriptFile() : base(PluginFolder + "oembed.js") { }
        }

        public class OEmbedCssFile : CssFile
        {
            public OEmbedCssFile() : base(PluginFolder + "oembed.css") { }
        }

        private const string PluginFolder = "/App_Plugins/" + PropertyEditorAlias + "/";
        public const string PropertyEditorAlias = "Novicell.OEmbed";
        public const string PluginAreaName = "Novicell";

        private readonly IIOHelper _ioHelper;

        public OEmbedPropertyEditor(
            IDataValueEditorFactory dataValueEditorFactory,
            IIOHelper ioHelper)
            : base(dataValueEditorFactory)
        {
            _ioHelper = ioHelper;
        }

        protected override IConfigurationEditor CreateConfigurationEditor()
        {
            return new OEmbedConfigurationEditor(_ioHelper);
        }
    }
}