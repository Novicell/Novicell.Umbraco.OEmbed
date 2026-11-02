using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Novicell.Umbraco.OEmbed.Migrations;

/// <summary>
/// Makes the data types of the Umbraco 9 version work with this version. They keep their editor alias, so
/// existing properties and content keep working; what changes is the editor UI they use and the setting
/// that limits the type, see <see cref="Umbraco9DataTypes"/>.
/// </summary>
/// <remarks>
/// It only touches data types with the editor alias of this package, and a data type that is already in the
/// current shape is left as it is, so it changes nothing on a new site. Content is not touched: a value of
/// the old version is read as it is, and saved in the current shape the next time it is edited.
/// The data types are saved through <see cref="IDataTypeService"/>, so Umbraco validates them and refreshes
/// its caches, and no table is written to directly.
/// If a data type cannot be saved the migration fails, after it has tried all of them, so Umbraco does not
/// record it as done and runs it again at the next start.
/// </remarks>
internal sealed class ConvertUmbraco9DataTypes(IMigrationContext context, IDataTypeService dataTypeService)
    : AsyncMigrationBase(context)
{
    protected override async Task MigrateAsync()
    {
        Result result = await ConvertAsync(dataTypeService);

        Logger.LogInformation(
            "oEmbed migration: {DataTypeCount} data types with the editor {EditorAlias}, {ChangedCount} converted",
            result.Found,
            Constants.PropertyEditors.OEmbedPicker,
            result.Changed);

        if (result.Failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"oEmbed migration: {result.Failures.Count} of {result.Found} data types could not be converted: "
                + string.Join(", ", result.Failures));
        }
    }

    /// <summary>
    /// Converts every data type that needs it. A data type that cannot be saved does not stop the others, and is
    /// named in <see cref="Result.Failures"/> with the reason.
    /// </summary>
    internal static async Task<Result> ConvertAsync(IDataTypeService dataTypeService)
    {
        IEnumerable<IDataType> dataTypes = await dataTypeService.GetByEditorAliasAsync(Constants.PropertyEditors.OEmbedPicker);

        var found = 0;
        var changed = 0;
        var failures = new List<string>();

        foreach (IDataType dataType in dataTypes)
        {
            found++;

            if (!Umbraco9DataTypes.Convert(dataType))
            {
                continue;
            }

            Attempt<IDataType, DataTypeOperationStatus> saved =
                await dataTypeService.UpdateAsync(dataType, global::Umbraco.Cms.Core.Constants.Security.SuperUserKey);

            if (saved.Success)
            {
                changed++;
            }
            else
            {
                failures.Add($"{dataType.Name} ({dataType.Key}): {saved.Status}");
            }
        }

        return new Result(found, changed, failures);
    }

    internal sealed record Result(int Found, int Changed, IReadOnlyList<string> Failures);
}
