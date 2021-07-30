using System;
using System.Linq;
using System.Net.Mime;

namespace Novicell.Umbraco.OEmbed.Services
{
    internal abstract class OEmbedServiceBase
	{
        private static string[] XmlMediaTypes
            => new[] { MediaTypeNames.Text.Xml, MediaTypeNames.Application.Xml };

        private static string[] JsonMediaTypes
            => new[] { MediaTypeNames.Application.Json };

        internal static bool IsJson(string mediaType)
            => IsAnyOf(mediaType, JsonMediaTypes);

        internal static bool IsXml(string mediaType)
            => IsAnyOf(mediaType, XmlMediaTypes);

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



        internal static bool IsJsonOrXmlWithOEmbedSuffix(string mediaType, string suffix = "+oembed")
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                return false;
            }

            var indexOfSuffix = mediaType.LastIndexOf(suffix);
            if(indexOfSuffix == -1)
			{
                return false;
			}

            mediaType = mediaType.Substring(0, indexOfSuffix);

            return IsJson(mediaType) || IsXml(mediaType);
        }
	}
}