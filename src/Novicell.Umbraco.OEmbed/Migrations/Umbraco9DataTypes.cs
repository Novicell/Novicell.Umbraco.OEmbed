using System.Text.Json;
using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core.Models;

namespace Novicell.Umbraco.OEmbed.Migrations;

/// <summary>
/// What a data type of the Umbraco 9 version needs to work with this version: it keeps the editor alias, but
/// it has no editor UI of its own, and its one setting has another name and shape.
/// </summary>
internal static class Umbraco9DataTypes
{
    /// <summary>The alias of the property editor UI in the backoffice, see the client manifest.</summary>
    public const string EditorUiAlias = "Novicell.PropertyEditorUi.OEmbedPicker";

    /// <summary>
    /// Updates the data type, and returns whether anything changed. It changes nothing a second time, and
    /// nothing that is already in the current shape.
    /// </summary>
    public static bool Convert(IDataType dataType)
    {
        var changed = false;

        // Without a UI alias Umbraco falls back to the editor alias, and no UI is registered under that name.
        if (string.IsNullOrWhiteSpace(dataType.EditorUiAlias) || dataType.EditorUiAlias == dataType.EditorAlias)
        {
            dataType.EditorUiAlias = EditorUiAlias;
            changed = true;
        }

        if (ConvertConfiguration(dataType.ConfigurationData) is { } configuration)
        {
            dataType.ConfigurationData = configuration;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Converts the setting of the old version, <c>{ "Type": "Video" }</c>, to the allowed types, or returns
    /// null when the configuration is anything else (including the current shape).
    /// </summary>
    internal static IDictionary<string, object>? ConvertConfiguration(IDictionary<string, object>? configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        object? type = null;
        var hasType = false;

        foreach ((string key, object value) in configuration)
        {
            if (key.Equals("allowedTypes", StringComparison.OrdinalIgnoreCase)
                || key.Equals("multiple", StringComparison.OrdinalIgnoreCase)
                || key.Equals("validationLimit", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (key.Equals("type", StringComparison.OrdinalIgnoreCase))
            {
                hasType = true;
                type = value;
            }
        }

        if (!hasType)
        {
            return null;
        }

        // Umbraco gives the values as read from the JSON, so a text can be a string or a JsonElement.
        var text = type switch
        {
            string value => value,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null,
        };

        // Empty, the old "(Any)", and anything that is not one of the four types means no restriction.
        OEmbedType? allowed = Enum.GetValues<OEmbedType>()
            .Cast<OEmbedType?>()
            .FirstOrDefault(candidate => string.Equals(candidate.ToString(), text, StringComparison.OrdinalIgnoreCase));

        return new Dictionary<string, object>
        {
            ["allowedTypes"] = allowed is { } value2 ? new[] { value2.ToApiValue() } : Array.Empty<string>(),
        };
    }
}
