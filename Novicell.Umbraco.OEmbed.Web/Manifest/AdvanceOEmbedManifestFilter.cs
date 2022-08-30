using Umbraco.Cms.Core.Manifest;

namespace Novicell.OEmbed.Web.Manifest
{
    /// <inheritdoc />
    public class AdvanceOEmbedManifestFilter : IManifestFilter
    {
        /// <inheritdoc />
        public void Filter(List<PackageManifest> manifests)
        {
            manifests.Add(new PackageManifest
            {
                PackageName = Novicell.OEmbed.Core.Constants.Package.Alias,
                BundleOptions = BundleOptions.Independent,
                Scripts = new[]
                {
                    Core.Constants.Package.PluginFolder + "Scripts/Controller/OEmbed.Controller.js"
                },
                Stylesheets = new[]
                {
                    Core.Constants.Package.PluginFolder + "Styles/OEmbed.css"
                },
                Version = Novicell.OEmbed.Core.Constants.Package.Version
            });
        }
    }
}
