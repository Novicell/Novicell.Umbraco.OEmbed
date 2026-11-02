using Umbraco.Cms.Core.Packaging;

namespace Novicell.Umbraco.OEmbed.Migrations;

/// <summary>
/// Umbraco finds this plan and runs it once, at the first startup after the package is installed or updated,
/// and records that it has run.
/// </summary>
internal sealed class OEmbedMigrationPlan()
    : PackageMigrationPlan(Constants.PackageName, Constants.PackageName, Constants.PackageName)
{
    protected override void DefinePlan()
        => To<ConvertUmbraco9DataTypes>(new Guid("D3B0A9C4-6E2F-4F5B-9A17-5C8E1B2A7F40"));
}
