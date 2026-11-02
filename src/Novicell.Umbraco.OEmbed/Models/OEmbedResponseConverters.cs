using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novicell.Umbraco.OEmbed.Models;

// Providers are not strict about the kind of value a field has: TikTok sends the sizes as text, SoundCloud
// sends the version as a number, and some leave a field as something else entirely. These converters read
// a field of an unexpected kind as null, so one odd value never makes the whole response unreadable. Only
// the type is required (see OEmbedTypeConverter). They are used by OEmbedResponse, which is both what a
// provider returned and what is stored.

/// <summary>A number, or a number sent as text; anything else, or a value that is not a whole number in range, is null.</summary>
internal sealed class LenientIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return AsInt(reader.GetDouble());

            case JsonTokenType.String
                when double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number):
                return AsInt(number);

            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is { } number)
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    private static int? AsInt(double number)
        => Math.Floor(number) == number && number is >= int.MinValue and <= int.MaxValue ? (int)number : null;
}

/// <summary>Text; anything else is null.</summary>
internal sealed class LenientStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

/// <summary>The version is text, but some providers send it as a number (<c>1.0</c>); it is read as the text it was sent as.</summary>
internal sealed class LenientVersionConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.Number:
                using (JsonDocument number = JsonDocument.ParseValue(ref reader))
                {
                    return number.RootElement.GetRawText();
                }

            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

/// <summary>A URL, absolute or relative, as text; anything else, or text that is not a URL, is null. It is written as it was read.</summary>
internal sealed class LenientUriConverter : JsonConverter<Uri?>
{
    public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String
            && Uri.TryCreate(reader.GetString(), UriKind.RelativeOrAbsolute, out Uri? uri))
        {
            return uri;
        }

        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, Uri? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value?.OriginalString);
}

/// <summary>
/// The resource type, as text in any casing. It is the one field that must be right: anything else
/// (a number, an unknown type) is an error, so the response, or the stored item, is not used.
/// </summary>
internal sealed class OEmbedTypeConverter : JsonConverter<OEmbedType>
{
    public override OEmbedType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Only a name, in any casing. Enum.TryParse is not used because it also takes numbers (with a sign or
        // a space), combinations such as "video, photo" and padded text.
        if (reader.TokenType == JsonTokenType.String && reader.GetString() is { } text)
        {
            foreach (OEmbedType type in Enum.GetValues<OEmbedType>())
            {
                if (string.Equals(type.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    return type;
                }
            }
        }

        throw new JsonException("The oEmbed type is missing or is not video, photo, link or rich.");
    }

    public override void Write(Utf8JsonWriter writer, OEmbedType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToApiValue());
}

/// <summary>
/// Reads a value of <typeparamref name="T"/> that is not usable (for example the response of a stored item
/// that has no valid type) as null, so the item is reported as unusable instead of the read failing.
/// </summary>
internal sealed class TolerantObjectConverter<T> : JsonConverter<T?>
    where T : class
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return null;
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);

        try
        {
            return document.RootElement.Deserialize<T>();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value);
}
