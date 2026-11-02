using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Novicell.Umbraco.OEmbed.Api.Configuration;
using Novicell.Umbraco.OEmbed.EmbedProviders;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.PropertyEditors;
using Novicell.Umbraco.OEmbed.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core.DependencyInjection;

namespace Novicell.Umbraco.OEmbed.Configuration;

public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the OEmbed services, the value converter, the extra embed providers and the Management API
    /// the backoffice uses to the Umbraco builder. <see cref="Composers.OEmbedComposer"/> calls this, so it
    /// is only needed when the package is registered by hand. Calling it more than once is safe: the
    /// package is registered the first time and later calls do nothing.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The same builder.</returns>
    public static IUmbracoBuilder AddNovicellOEmbed(this IUmbracoBuilder builder)
    {
        IServiceCollection services = builder.Services;

        // The composer registers the package, so a site that also calls this by hand would register it twice.
        if (services.Any(descriptor => descriptor.ServiceType == typeof(RegistrationMarker)))
        {
            return builder;
        }

        services.AddSingleton<RegistrationMarker>();
        services.AddOEmbedHttpClient();
        services.AddTransient<IOEmbedApiEndpointProvider, OEmbedApiEndpointProvider>();
        services.AddTransient<IOEmbedService, OEmbedService>();
        services.AddOptions<OEmbedOptions>()
            .Bind(builder.Config.GetSection(OEmbedOptions.SectionKey));

        builder.PropertyValueConverters().Append<OEmbedPickerPropertyValueConverter>();

        builder.EmbedProviders()
            .Append<Reddit>()
            .Append<Spotify>()
            .Append<TikTok>();

        // The Management API: its swagger document, which the generated backoffice client is built from.
        services.Configure<SwaggerGenOptions>(options =>
        {
            options.SwaggerDoc(Constants.ApiName, new OpenApiInfo { Title = "Novicell OEmbed API", Version = "1.0" });
            options.OperationFilter<OEmbedApiOperationSecurityFilter>();
        });
        services.AddSingleton<IOperationIdHandler, OEmbedApiOperationIdHandler>();

        return builder;
    }

    /// <summary>Tells later calls that the package is already registered.</summary>
    internal sealed class RegistrationMarker;
}
