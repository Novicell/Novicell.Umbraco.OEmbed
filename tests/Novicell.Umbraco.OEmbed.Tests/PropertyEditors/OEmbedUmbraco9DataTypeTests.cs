using System.Text.Json;
using Novicell.Umbraco.OEmbed.Migrations;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.Tests.PropertyEditors;

/// <summary>
/// The editor keeps the alias of the Umbraco 9 version, so its data types and properties keep working. The
/// migration gives those data types the editor UI and the setting they need in this version.
/// </summary>
[TestFixture]
public class OEmbedUmbraco9DataTypeTests
{
    private const string OldUiAliasFallback = "Novicell.OEmbed";
    private const string CurrentUiAlias = "Novicell.PropertyEditorUi.OEmbedPicker";

    // The configuration as Umbraco hands it over: the stored JSON as a dictionary, with its values as read.
    private static IDataType DataType(string? uiAlias, string configuration)
    {
        IDataType dataType = Substitute.For<IDataType>();
        dataType.EditorAlias.Returns(Constants.PropertyEditors.OEmbedPicker);
        dataType.EditorUiAlias = uiAlias;
        dataType.ConfigurationData = JsonSerializer.Deserialize<Dictionary<string, object>>(configuration)!;
        return dataType;
    }

    private static string AllowedTypes(IDataType dataType)
        => string.Join(",", (string[])dataType.ConfigurationData["allowedTypes"]);

    [Test]
    public void The_Alias_Is_The_One_Of_The_Umbraco_9_Version()
    {
        // Data types and properties are saved with this alias, so changing it breaks every site that has them.
        Assert.That(Constants.PropertyEditors.OEmbedPicker, Is.EqualTo("Novicell.OEmbed"));
        Assert.That(
            typeof(OEmbedPickerPropertyEditor).GetCustomAttributes(typeof(DataEditorAttribute), inherit: false)
                .Cast<DataEditorAttribute>().Single().Alias,
            Is.EqualTo("Novicell.OEmbed"));
    }

    [TestCase("""{"Type":"Video"}""", "video")]
    [TestCase("""{"Type":"Photo"}""", "photo")]
    [TestCase("""{"Type":"Rich"}""", "rich")]
    [TestCase("""{"Type":"Link"}""", "link")]
    [TestCase("""{ "type": "video" }""", "video")]
    [TestCase("""{"Type":""}""", "")]
    [TestCase("""{"Type":null}""", "")]
    [TestCase("""{"Type":3}""", "")]
    [TestCase("""{"Type":"movie"}""", "")]
    public void The_Type_Setting_Becomes_The_Allowed_Types(string stored, string expected)
    {
        IDataType dataType = DataType(CurrentUiAlias, stored);

        Assert.That(Umbraco9DataTypes.Convert(dataType), Is.True);
        Assert.That(AllowedTypes(dataType), Is.EqualTo(expected));
        Assert.That(dataType.ConfigurationData, Has.Count.EqualTo(1), "the old setting is gone");
    }

    [Test]
    public void The_Type_Is_Read_Whether_Umbraco_Gives_It_As_A_String_Or_As_JSON()
    {
        IDataType dataType = DataType(CurrentUiAlias, """{"Type":"Video"}""");
        dataType.ConfigurationData = new Dictionary<string, object> { ["Type"] = "Photo" };

        Umbraco9DataTypes.Convert(dataType);

        Assert.That(AllowedTypes(dataType), Is.EqualTo("photo"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(OldUiAliasFallback)]
    public void A_Data_Type_Without_An_Editor_UI_Gets_Ours(string? uiAlias)
    {
        IDataType dataType = DataType(uiAlias, """{"allowedTypes":["video"]}""");

        Assert.That(Umbraco9DataTypes.Convert(dataType), Is.True);
        Assert.That(dataType.EditorUiAlias, Is.EqualTo(CurrentUiAlias));
        Assert.That(dataType.ConfigurationData.Keys, Is.EquivalentTo(new[] { "allowedTypes" }), "the configuration is already in the current shape");
    }

    [Test]
    public void An_Editor_UI_That_Was_Chosen_Is_Not_Replaced()
    {
        IDataType dataType = DataType("Some.Other.PropertyEditorUi", """{"Type":"Video"}""");

        Umbraco9DataTypes.Convert(dataType);

        Assert.That(dataType.EditorUiAlias, Is.EqualTo("Some.Other.PropertyEditorUi"));
        Assert.That(AllowedTypes(dataType), Is.EqualTo("video"));
    }

    [Test]
    public void A_Data_Type_From_The_Old_Version_Is_Converted_Once()
    {
        IDataType dataType = DataType(null, """{"Type":"Video"}""");

        Assert.That(Umbraco9DataTypes.Convert(dataType), Is.True);
        Assert.That(Umbraco9DataTypes.Convert(dataType), Is.False, "nothing is left to convert");
        Assert.That(dataType.EditorUiAlias, Is.EqualTo(CurrentUiAlias));
        Assert.That(AllowedTypes(dataType), Is.EqualTo("video"));
    }

    [TestCase("""{"allowedTypes":["video","rich"],"multiple":true,"validationLimit":{"min":1,"max":3}}""")]
    [TestCase("""{"Type":"Video","allowedTypes":["photo"],"multiple":false}""")]
    [TestCase("""{"multiple":true}""")]
    [TestCase("""{}""")]
    public void A_Data_Type_In_The_Current_Shape_Is_Left_As_It_Is(string configuration)
    {
        IDataType dataType = DataType(CurrentUiAlias, configuration);
        Dictionary<string, object> before = new(dataType.ConfigurationData);

        Assert.That(Umbraco9DataTypes.Convert(dataType), Is.False);
        Assert.That(dataType.ConfigurationData, Is.EqualTo(before));
        Assert.That(dataType.EditorUiAlias, Is.EqualTo(CurrentUiAlias));
    }

    [Test]
    public void The_Editor_UI_Alias_Is_The_One_In_The_Client_Manifest()
    {
        // A different alias here would point the migrated data types at an editor UI that does not exist.
        var manifest = System.IO.File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "Novicell.Umbraco.OEmbed", "Client", "src", "property-editors", "oembed-picker", "manifests.ts"));

        Assert.That(manifest, Does.Contain($"alias: '{Umbraco9DataTypes.EditorUiAlias}'"));
    }
}
