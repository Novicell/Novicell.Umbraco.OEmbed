using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.PropertyEditors;

/// <summary>
/// Represents the configuration editor for the OEmbed picker value editor.
/// </summary>
internal class OEmbedPickerConfigurationEditor : ConfigurationEditor<OEmbedPickerConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OEmbedPickerConfigurationEditor" /> class.
    /// </summary>
    public OEmbedPickerConfigurationEditor(IIOHelper ioHelper)
        : base(ioHelper)
    {
    }
}
