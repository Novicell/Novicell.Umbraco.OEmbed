using System;
using Newtonsoft.Json;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Extensions;

namespace Novicell.Umbraco.OEmbed.PropertyValueConverters
{
    public class OEmbedPropertyValueConverter : PropertyValueConverterBase, IPropertyValueConverter 
    {
        /// <summary>
        /// Gets a value indicating whether the converter supports a property type.
        /// </summary>
        /// <param name="propertyType">The property type.</param>
        /// <returns>A value indicating whether the converter supports a property type.</returns>
        public override bool IsConverter(IPublishedPropertyType propertyType)
        {
            return propertyType.EditorAlias == OEmbedPropertyEditor.PropertyEditorAlias;
        }

        /// <summary>
        /// Gets the type of values returned by the converter.
        /// </summary>
        /// <param name="propertyType">The property type.</param>
        /// <returns>The CLR type of values returned by the converter.</returns>
        public override Type GetPropertyValueType(IPublishedPropertyType propertyType)
            => typeof(OEmbedValue);

        public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) 
            => PropertyCacheLevel.Element;

        public override object ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType propertyType,
            PropertyCacheLevel referenceCacheLevel, object inter, bool preview)
        {
            if (inter == null)
                return null;

            var model = JsonConvert.DeserializeObject<OEmbedValue>((string)inter);

            return model;
        }

        public override object ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType propertyType, object source,
            bool preview)
        {
            if (!(source is string json) ||
                string.IsNullOrWhiteSpace(json) ||
                !json.DetectIsJson())
            {
                return null;
            }

            return json;
        }
    }
}