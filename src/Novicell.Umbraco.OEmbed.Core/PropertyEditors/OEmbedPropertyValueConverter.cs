using System;
using Newtonsoft.Json;
using Novicell.Umbraco.OEmbed.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Extensions;

namespace Novicell.Umbraco.OEmbed.Core.PropertyEditors
{
    public class OEmbedPropertyValueConverter : PropertyValueConverterBase
    {
        /// <inheritdoc/>
        public override bool IsConverter(IPublishedPropertyType propertyType)
            => propertyType.EditorAlias == OEmbedPropertyEditor.PropertyEditorAlias;

        /// <inheritdoc/>
        public override Type GetPropertyValueType(IPublishedPropertyType propertyType)
            => typeof(OEmbedValue);

        /// <inheritdoc/>
        public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType)
            => PropertyCacheLevel.Element;

        /// <inheritdoc/>
        public override object ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType propertyType,
            PropertyCacheLevel referenceCacheLevel, object inter, bool preview)
        {
            if (inter == null)
                return null;

            var model = JsonConvert.DeserializeObject<OEmbedValue>((string)inter);

            return model;
        }

        /// <inheritdoc/>
        public override object ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType propertyType, object source,
            bool preview)
        {
            if (source is not string json ||
                string.IsNullOrWhiteSpace(json) ||
                !json.DetectIsJson())
            {
                return null;
            }

            return json;
        }
    }
}