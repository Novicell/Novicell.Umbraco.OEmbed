using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.PropertyEditors
{
    internal class OEmbedConfigurationTypeFieldValidator : IManifestValueValidator
    {
        private readonly string _memberName;

        public OEmbedConfigurationTypeFieldValidator(string memberName)
        {
            _memberName = memberName;
        }

        public IEnumerable<ValidationResult> Validate(object value, string valueType, object dataTypeConfiguration)
        {
            if (value?.ToString() is not { } s || string.IsNullOrWhiteSpace(s))
            {
                yield break;
            }

            if (!Enum.TryParse(s, true, out OEmbedType _))
            {
                yield return new ValidationResult("The value " + value + " is not a valid OEmbed type",
                    new[] {_memberName});
            }
        }

        public string ValidationName => "OEmbedType";
    }
}