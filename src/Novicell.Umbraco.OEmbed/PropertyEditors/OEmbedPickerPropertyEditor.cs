using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.PropertyEditors;

/// <inheritdoc />
[DataEditor(Constants.PropertyEditors.OEmbedPicker, ValueEditorIsReusable = true, ValueType = ValueTypes.Json)]
internal class OEmbedPickerPropertyEditor : DataEditor
{
    private readonly IIOHelper _ioHelper;

    /// <inheritdoc />
    public OEmbedPickerPropertyEditor(IDataValueEditorFactory dataValueEditorFactory, IIOHelper ioHelper)
        : base(dataValueEditorFactory)
    {
        _ioHelper = ioHelper;
    }

    /// <summary>The index gets what an embed is about, not the stored JSON, see <see cref="OEmbedPickerPropertyIndexValueFactory"/>.</summary>
    public override IPropertyIndexValueFactory PropertyIndexValueFactory { get; } = new OEmbedPickerPropertyIndexValueFactory();

    /// <inheritdoc />
    protected override IConfigurationEditor CreateConfigurationEditor()
        => new OEmbedPickerConfigurationEditor(_ioHelper);

    /// <inheritdoc />
    protected override IDataValueEditor CreateValueEditor()
        => DataValueEditorFactory.Create<OEmbedPickerPropertyValueEditor>(Attribute!);
}
