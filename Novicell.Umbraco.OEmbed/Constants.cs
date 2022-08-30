using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novicell.OEmbed.Core
{
    /// <summary>
    /// Defines constants.
    /// </summary>
    public static partial class Constants
    {
        /// <summary>
        /// Defines constants for gernal packeage data.
        /// </summary>
        public static class Package
        {
            public const string Name = "Novicell.Umbraco.OEmbed";
            public const string Alias = "novicellUmbracoOEmbed";
            public const string Version = "2.0.0";
            public const string PluginFolder = $"/App_Plugins/{Alias}/";
            public const string PluginAreaName = "Novicell";
        }
    }
}
