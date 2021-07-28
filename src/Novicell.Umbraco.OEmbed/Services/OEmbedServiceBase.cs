using System;
using System.Linq;
using System.Net.Mime;

namespace Novicell.Umbraco.OEmbed.Services
{
    internal abstract class OEmbedServiceBase
	{
        private static string[] XmlMediaTypes => new[] {
                MediaTypeNames.Text.Xml,
                MediaTypeNames.Application.Xml
            };

        private static string[] JsonMediaTypes => new[] {
                MediaTypeNames.Application.Json
            };


        protected static bool IsJson(string mediaType) => IsAnyOf(mediaType, JsonMediaTypes);

        protected static bool IsXml(string mediaType) => IsAnyOf(mediaType, XmlMediaTypes);

        private static bool IsAnyOf(string mediaType, params string[] mediaTypes)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                return false;
            }

            return mediaTypes.Any(x => x.Equals(mediaType, StringComparison.InvariantCultureIgnoreCase));
        }

        protected static bool IsJsonOrXmlWithOEmbedSuffix(string mediaType, string suffix = "+oembed")
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

		protected static bool IsAlternateOrAlternative(string rel) => IsAnyOf(rel, "alternate", "alternative");

	}
}