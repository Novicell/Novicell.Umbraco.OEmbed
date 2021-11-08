﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Novicell.Umbraco.OEmbed.Core.Configuration.Models;
using Novicell.Umbraco.OEmbed.Core.PropertyEditors;
using Novicell.Umbraco.OEmbed.Core.Services;
using Umbraco.Cms.Core.DependencyInjection;

namespace Novicell.Umbraco.OEmbed.Core
{
    public static class UmbracoBuilderExtensions
    {
        public static IUmbracoBuilder AddNovicellOEmbed(this IUmbracoBuilder builder,
            Action<OEmbedSettings> defaultOptions = default)
        {
            // load up the settings.
            var options = builder.Services.AddOptions<OEmbedSettings>()
                .Bind(builder.Config.GetSection(OEmbedSettings.SectionKey));

            if (defaultOptions != default)
            {
                options.Configure(defaultOptions);
            }

            options.ValidateDataAnnotations();

            builder.BackOfficeAssets()
                .Append<OEmbedPropertyEditor.OEmbedJavaScriptFile>()
                .Append<OEmbedPropertyEditor.OEmbedCssFile>();

            builder.Services.AddScoped<IOEmbedService, OEmbedService>();

            builder.Services.AddScoped<IOEmbedDiscoveryService, IOEmbedDiscoveryService>();

            return builder;
        }
    }
}