using System;
using System.Linq;
using System.Net.Mime;

namespace Novicell.Umbraco.OEmbed.Services
{
    internal abstract class OEmbedServiceBase
	{
        internal static string OEmbedMediaTypeSuffix = "+oembed";

        private static readonly string[] XmlMediaTypes
            = new[] { MediaTypeNames.Text.Xml, MediaTypeNames.Application.Xml };

        private static readonly string[] JsonMediaTypes
            = new[] { MediaTypeNames.Application.Json };

        internal static bool IsJson(string mediaType, string suffix = null)
            => IsAnyOfWithSuffix(mediaType, suffix ?? string.Empty, JsonMediaTypes);

        internal static bool IsXml(string mediaType, string suffix = null)
            => IsAnyOfWithSuffix(mediaType, suffix??string.Empty, XmlMediaTypes);

        internal static bool IsAlternateOrAlternative(string rel)
            => IsAnyOf(rel, "alternate", "alternative");

        internal static bool IsAnyOf(string input, params string[] options)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            return options.Any(x => x.Equals(input, StringComparison.InvariantCultureIgnoreCase));
        }

        internal static bool IsAnyOfWithSuffix(string input, string suffix, params string[] options)
        {
            return IsAnyOf(input, options.Select(x => x + suffix).ToArray());
        }
	}
}