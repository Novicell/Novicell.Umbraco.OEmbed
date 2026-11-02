using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.PropertyEditors;

/// <summary>
/// Represents the configuration for the OEmbed picker value editor.
/// </summary>
internal class OEmbedPickerConfiguration
{
    /// <summary>
    /// Gets or sets whether more than one embed can be picked for this property.
    /// </summary>
    [ConfigurationField("multiple")]
    public bool Multiple { get; set; }

    /// <summary>
    /// Gets or sets the validation limits for the number of embeds.
    /// </summary>
    [ConfigurationField("validationLimit")]
    public NumberRange ValidationLimit { get; set; } = new();

    /// <summary>
    /// Gets or sets the oEmbed types allowed for this property. Empty means no restriction.
    /// </summary>
    [ConfigurationField("allowedTypes")]
    public string[] AllowedTypes { get; set; } = [];

    /// <summary>
    /// Represents a numeric range with optional minimum and maximum values.
    /// </summary>
    internal class NumberRange
    {
        /// <summary>
        /// Gets or sets the minimum value of the range.
        /// </summary>
        public int? Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum value of the range.
        /// </summary>
        public int? Max { get; set; }
    }
}
