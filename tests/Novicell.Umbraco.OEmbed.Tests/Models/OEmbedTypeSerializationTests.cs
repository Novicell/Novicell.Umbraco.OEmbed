using System.Text.Json;
using System.Text.Json.Serialization;
using Novicell.Umbraco.OEmbed.Models;
using NUnit.Framework;

namespace Novicell.Umbraco.OEmbed.Tests.Models;

[TestFixture]
public class OEmbedTypeSerializationTests
{
    private static JsonSerializerOptions CreateOptions() => new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [TestCase(OEmbedType.Video, "video")]
    [TestCase(OEmbedType.Photo, "photo")]
    [TestCase(OEmbedType.Link, "link")]
    [TestCase(OEmbedType.Rich, "rich")]
    public void Serializes_To_Lowercase_Spec_Value(OEmbedType type, string expected)
    {
        var json = JsonSerializer.Serialize(type, CreateOptions());

        Assert.That(json, Is.EqualTo($"\"{expected}\""));
    }

    [TestCase("video", OEmbedType.Video)]
    [TestCase("photo", OEmbedType.Photo)]
    [TestCase("link", OEmbedType.Link)]
    [TestCase("rich", OEmbedType.Rich)]
    public void Deserializes_From_Lowercase_Spec_Value(string value, OEmbedType expected)
    {
        var result = JsonSerializer.Deserialize<OEmbedType>($"\"{value}\"", CreateOptions());

        Assert.That(result, Is.EqualTo(expected));
    }
}
