using System.ComponentModel.DataAnnotations;
using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models.Validation;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.Validation;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace Novicell.Umbraco.OEmbed.PropertyEditors;

/// <summary>
/// The value editor of the OEmbed picker. The value is stored as the backoffice posted it: the URL the
/// editor entered and the provider's response, see <see cref="OEmbedDto"/>.
/// </summary>
/// <remarks>
/// The validators are nested here, like Umbraco's Media Picker 3 nests its own in
/// <c>MediaPicker3PropertyEditor.MediaPicker3PropertyValueEditor</c>: this is the only place they are used.
/// </remarks>
internal sealed class OEmbedPickerPropertyValueEditor : DataValueEditor
{
    /// <inheritdoc />
    public OEmbedPickerPropertyValueEditor(
        IShortStringHelper shortStringHelper,
        IJsonSerializer jsonSerializer,
        IIOHelper ioHelper,
        DataEditorAttribute attribute,
        ILocalizedTextService localizedTextService)
        : base(shortStringHelper, jsonSerializer, ioHelper, attribute)
    {
        var validators = new TypedJsonValidatorRunner<List<OEmbedDto>, OEmbedPickerConfiguration>(
            jsonSerializer,
            new MinMaxValidator(localizedTextService),
            new AllowedTypeValidator(localizedTextService),
            new UrlValidator(localizedTextService));

        Validators.Add(validators);
    }

    /// <summary>
    /// Validates the number of embeds on an OEmbed picker property against its configured
    /// <see cref="OEmbedPickerConfiguration.Multiple"/> and <see cref="OEmbedPickerConfiguration.ValidationLimit"/>
    /// (min/max), mirroring Umbraco's own Media Picker 3 <c>MinMaxValidator</c>
    /// (<c>MediaPicker3PropertyEditor.MediaPicker3PropertyValueEditor.MinMaxValidator</c>), including its
    /// <c>validation_multipleMediaNotAllowed</c>/<c>validation_entriesShort</c>/<c>validation_entriesExceed</c> checks.
    /// </summary>
    internal sealed class MinMaxValidator : ITypedJsonValidator<List<OEmbedDto>, OEmbedPickerConfiguration>
    {
        private readonly ILocalizedTextService _localizedTextService;

        public MinMaxValidator(ILocalizedTextService localizedTextService)
            => _localizedTextService = localizedTextService;

        public IEnumerable<ValidationResult> Validate(
            List<OEmbedDto>? value,
            OEmbedPickerConfiguration? configuration,
            string? valueType,
            PropertyValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (value is null || configuration is null)
            {
                return results;
            }

            if (configuration.Multiple is false && value.Count > 1)
            {
                results.Add(new ValidationResult(
                    _localizedTextService.Localize("validation", "multipleMediaNotAllowed"),
                    ["value"]));
            }

            if (configuration.ValidationLimit.Min is int min && value.Count < min)
            {
                results.Add(new ValidationResult(
                    _localizedTextService.Localize("validation", "entriesShort", [min.ToString(), (min - value.Count).ToString()]),
                    ["value"]));
            }

            if (configuration.ValidationLimit.Max is int max && value.Count > max)
            {
                results.Add(new ValidationResult(
                    _localizedTextService.Localize("validation", "entriesExceed", [max.ToString(), (value.Count - max).ToString()]),
                    ["value"]));
            }

            return results;
        }
    }

    /// <summary>
    /// Validates the oEmbed types on an OEmbed picker property against its configured
    /// <see cref="OEmbedPickerConfiguration.AllowedTypes"/>, mirroring Umbraco's own Media Picker 3
    /// <c>AllowedTypeValidator</c> and Multi Node Tree Picker <c>ObjectTypeValidator</c>
    /// (reusing its <c>validation_invalidObjectType</c> message for a per-item type mismatch).
    /// </summary>
    internal sealed class AllowedTypeValidator : ITypedJsonValidator<List<OEmbedDto>, OEmbedPickerConfiguration>
    {
        private readonly ILocalizedTextService _localizedTextService;

        public AllowedTypeValidator(ILocalizedTextService localizedTextService)
            => _localizedTextService = localizedTextService;

        public IEnumerable<ValidationResult> Validate(
            List<OEmbedDto>? value,
            OEmbedPickerConfiguration? configuration,
            string? valueType,
            PropertyValidationContext validationContext)
        {
            if (value is null || configuration is null || configuration.AllowedTypes.Length == 0)
            {
                return [];
            }

            var allowed = new HashSet<string>(configuration.AllowedTypes, StringComparer.OrdinalIgnoreCase);
            var results = new List<ValidationResult>();

            // Posted JSON can contain explicit nulls (an item, or its oembed), which `required` does
            // not stop; they have no type, so they are not allowed.
            foreach (OEmbedDto? stored in value)
            {
                OEmbedResponse? response = stored?.OEmbed;

                if (response is null || !allowed.Contains(response.Type.ToApiValue()))
                {
                    results.Add(new ValidationResult(
                        _localizedTextService.Localize("validation", "invalidObjectType"),
                        ["value"]));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// Rejects an OEmbed picker value whose URLs are not http(s). The value is posted by the client and
    /// stored as is, so a script-capable URL such as <c>javascript:</c> or <c>data:</c> would otherwise be
    /// saved, and later rendered as a link or an image source by the backoffice or by a site.
    /// </summary>
    /// <remarks>
    /// The rule is <see cref="OEmbedUrlSafety"/>: the URL the editor entered must be an absolute http(s)
    /// URL, and the URLs inside the stored response (author, provider, thumbnail and resource) may also be
    /// relative, because some providers return those, but never an absolute URL with another scheme, and
    /// never a URL with a control character in it. A null item or response is rejected too.
    /// Reuses Umbraco's <c>validation_invalidPattern</c> message.
    /// This is the early, friendly check. The published value converter applies the same rule to what it
    /// produces, because a value does not only reach the database through this validator.
    /// </remarks>
    internal sealed class UrlValidator : ITypedJsonValidator<List<OEmbedDto>, OEmbedPickerConfiguration>
    {
        private readonly ILocalizedTextService _localizedTextService;

        public UrlValidator(ILocalizedTextService localizedTextService)
            => _localizedTextService = localizedTextService;

        public IEnumerable<ValidationResult> Validate(
            List<OEmbedDto>? value,
            OEmbedPickerConfiguration? configuration,
            string? valueType,
            PropertyValidationContext validationContext)
        {
            if (value is null)
            {
                return [];
            }

            // Posted JSON can contain explicit nulls (an item, or its oembed), which `required` does not
            // stop; IsSafe treats those as invalid.
            return value.Any(item => !OEmbedUrlSafety.IsSafe(item?.ToValue()))
                ? [new ValidationResult(_localizedTextService.Localize("validation", "invalidPattern"), ["value"])]
                : [];
        }
    }
}
