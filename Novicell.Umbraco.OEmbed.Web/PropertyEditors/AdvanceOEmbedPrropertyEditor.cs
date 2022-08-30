using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace Novicell.OEmbed.Web.PropertyEditors
{
    /// <summary>
    /// Represents a AdvanceOEmbed property editor.
    /// </summary>
    [DataEditor(
        alias: Novicell.OEmbed.Core.Constants.Package.Alias,
        name: Novicell.OEmbed.Core.Constants.Package.Name,
        view: Novicell.OEmbed.Core.Constants.Package.PluginFolder + "ombed.propertyeditor.html",
        Group = Umbraco.Cms.Core.Constants.PropertyEditors.Groups.Media,
        Icon = "icon-picture",
        ValueType = ValueTypes.Json)]
    public class AdvanceOEmbedPrropertyEditor: DataEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdvanceOEmbedPrropertyEditor"/> class.
        /// </summary>
        public AdvanceOEmbedPrropertyEditor(IDataValueEditorFactory dataValueEditorFactory)
            : base(dataValueEditorFactory)
        {
        }
    }
}
