using System.Text.Json;
using Novicell.Umbraco.OEmbed.Migrations;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Novicell.Umbraco.OEmbed.Tests.PropertyEditors;

/// <summary>
/// The migration saves the data types through Umbraco's service. One that cannot be saved must fail the
/// migration, otherwise Umbraco records it as done and never runs it again.
/// </summary>
[TestFixture]
public class OEmbedUmbraco9MigrationTests
{
    private IDataTypeService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IDataTypeService>();
        _service.UpdateAsync(Arg.Any<IDataType>(), Arg.Any<Guid>())
            .Returns(call => Attempt.SucceedWithStatus(DataTypeOperationStatus.Success, call.Arg<IDataType>()));
    }

    private static IDataType DataType(string name, string? uiAlias, string configuration)
    {
        IDataType dataType = Substitute.For<IDataType>();
        dataType.Name.Returns(name);
        dataType.Key.Returns(Guid.NewGuid());
        dataType.EditorAlias.Returns(Constants.PropertyEditors.OEmbedPicker);
        dataType.EditorUiAlias = uiAlias;
        dataType.ConfigurationData = JsonSerializer.Deserialize<Dictionary<string, object>>(configuration)!;
        return dataType;
    }

    private void Returns(params IDataType[] dataTypes)
        => _service.GetByEditorAliasAsync(Constants.PropertyEditors.OEmbedPicker)
            .Returns(Task.FromResult<IEnumerable<IDataType>>(dataTypes));

    [Test]
    public async Task Only_The_Data_Types_That_Need_It_Are_Saved()
    {
        IDataType old = DataType("Old", null, """{"Type":"Video"}""");
        IDataType current = DataType("Current", Umbraco9DataTypes.EditorUiAlias, """{"allowedTypes":["video"]}""");
        Returns(old, current);

        ConvertUmbraco9DataTypes.Result result = await ConvertUmbraco9DataTypes.ConvertAsync(_service);

        Assert.That(result.Found, Is.EqualTo(2));
        Assert.That(result.Changed, Is.EqualTo(1));
        Assert.That(result.Failures, Is.Empty);
        await _service.Received(1).UpdateAsync(old, Arg.Any<Guid>());
        await _service.DidNotReceive().UpdateAsync(current, Arg.Any<Guid>());
    }

    [Test]
    public async Task A_Data_Type_That_Cannot_Be_Saved_Is_Reported_And_The_Others_Are_Still_Saved()
    {
        IDataType broken = DataType("Broken", null, """{"Type":"Video"}""");
        IDataType fine = DataType("Fine", null, """{"Type":"Photo"}""");
        Returns(broken, fine);
        _service.UpdateAsync(broken, Arg.Any<Guid>())
            .Returns(Attempt.FailWithStatus(DataTypeOperationStatus.NotFound, broken));

        ConvertUmbraco9DataTypes.Result result = await ConvertUmbraco9DataTypes.ConvertAsync(_service);

        Assert.That(result.Changed, Is.EqualTo(1), "the one that could be saved");
        Assert.That(result.Failures, Has.Count.EqualTo(1));
        Assert.That(result.Failures[0], Does.Contain("Broken").And.Contain(broken.Key.ToString()).And.Contain("NotFound"));
        await _service.Received(1).UpdateAsync(fine, Arg.Any<Guid>());
    }

    [Test]
    public async Task Nothing_To_Convert_Is_Not_A_Failure()
    {
        Returns();

        ConvertUmbraco9DataTypes.Result result = await ConvertUmbraco9DataTypes.ConvertAsync(_service);

        Assert.That(result, Is.EqualTo(new ConvertUmbraco9DataTypes.Result(0, 0, [])).Using<ConvertUmbraco9DataTypes.Result>(
            (a, b) => a.Found == b.Found && a.Changed == b.Changed && a.Failures.Count == b.Failures.Count));
    }
}
