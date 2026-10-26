using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.PropertyEditors
{
    /// <inheritdoc />
    [DataEditor(PropertyEditorAlias, ValueType = ValueTypes.Json)]
    internal class OEmbedPropertyEditor : DataEditor
    {
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