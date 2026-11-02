using Novicell.Umbraco.OEmbed;
using Umbraco.Cms.Api.Management.OpenApi;

namespace Novicell.Umbraco.OEmbed.Api.Configuration;

/// <inheritdoc />
internal class OEmbedApiOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
{
    /// <inheritdoc />
    protected override string ApiName => Constants.ApiName;
}
