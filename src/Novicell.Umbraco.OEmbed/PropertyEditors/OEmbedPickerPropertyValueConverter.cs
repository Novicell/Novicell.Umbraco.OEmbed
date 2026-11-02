using Novicell.Umbraco.OEmbed.DeliveryApi;
using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.DeliveryApi;

namespace Novicell.Umbraco.OEmbed.PropertyEditors;

/// <summary>
/// Converts OEmbed picker property values to <see cref="OEmbedValue"/>: a single value, or null
/// when there is none, when the data type does not allow multiple embeds, and a collection when it does
/// (like Umbraco's media picker).
/// </summary>
/// <remarks>
/// The property value is stored as JSON: <c>[{ "url": "...", "oembed": { ... } }, ...]</c>. Legacy
/// content stored before multi-value support (a single <c>{ "url": ..., "oembed": ... }</c> object)
/// is tolerated transparently via <see cref="OEmbedDto.Deserialize"/>. If a data type was
/// changed from multiple to single after content was saved, the first embed is the value.
/// For the Delivery API the value is always an array of <see cref="ApiOEmbedValue"/>, a contract of its own
/// that does not follow the Razor model, so changing the setting does not change what a site receives.
/// </remarks>
internal class OEmbedPickerPropertyValueConverter : PropertyValueConverterBase, IDeliveryApiPropertyValueConverter
{
    /// <inheritdoc />
    public override bool IsConverter(IPublishedPropertyType propertyType)
        => propertyType.EditorAlias == Constants.PropertyEditors.OEmbedPicker;

    /// <inheritdoc />
    public override Type GetPropertyValueType(IPublishedPropertyType propertyType)
        => IsMultiple(propertyType) ? typeof(IEnumerable<OEmbedValue>) : typeof(OEmbedValue);

    /// <inheritdoc />
    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType)
        => PropertyCacheLevel.Element;

    /// <inheritdoc />
    public override object? ConvertSourceToIntermediate(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        object? source,
        bool preview)
        => OEmbedDto.ToValues(source as string);

    /// <inheritdoc />
    public override object? ConvertIntermediateToObject(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        PropertyCacheLevel referenceCacheLevel,
        object? inter,
        bool preview)
        => inter is IEnumerable<OEmbedValue> values
            ? IsMultiple(propertyType) ? values : values.FirstOrDefault()
            : null;

    private static bool IsMultiple(IPublishedPropertyType propertyType)
        => propertyType.DataType?.ConfigurationAs<OEmbedPickerConfiguration>()?.Multiple ?? false;

    /// <inheritdoc />
    public PropertyCacheLevel GetDeliveryApiPropertyCacheLevel(IPublishedPropertyType propertyType)
        => PropertyCacheLevel.Element;

    /// <inheritdoc />
    public Type GetDeliveryApiPropertyValueType(IPublishedPropertyType propertyType)
        => typeof(IEnumerable<ApiOEmbedValue>);

    /// <inheritdoc />
    public object? ConvertIntermediateToDeliveryApiObject(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        PropertyCacheLevel referenceCacheLevel,
        object? inter,
        bool preview,
        bool expanding)
        => (inter as IEnumerable<OEmbedValue> ?? [])
            .Select(ApiOEmbedValue.From)
            .ToArray();
}
