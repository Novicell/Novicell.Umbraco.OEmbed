using System;
using System.Collections.Generic;
using System.Linq;
using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.PropertyEditors
{
    public class OEmbedConfigurationEditor : ConfigurationEditor<OEmbedConfiguration>
    {
        public OEmbedConfigurationEditor(IIOHelper ioHelper) : base(ioHelper)
        {
            var type = Field(nameof(OEmbedConfiguration.Type));
            ConfigureTypeField(type);
        }

        private static void ConfigureTypeField(ConfigurationField field)
        {
            var types = new[]
            {
                Tuple.Create(nameof(OEmbedType.Video), "Video"),
                Tuple.Create(nameof(OEmbedType.Photo), "Photo"),
                Tuple.Create(nameof(OEmbedType.Rich), "Rich"),
                Tuple.Create(nameof(OEmbedType.Link), "Link"),
                Tuple.Create("", "(Any)"),
            };

            field.Validators.Add(new OEmbedConfigurationTypeFieldValidator(field.Key));
            field.Config = new Dictionary<string, object>
            {
                {
                    "prevalues", types
                        .Select(x => new {key = x.Item1, value = x.Item1, label = x.Item2})
                }
            };
        }
    }
}