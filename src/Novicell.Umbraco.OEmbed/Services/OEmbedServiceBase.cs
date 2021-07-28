using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Novicell.Umbraco.OEmbed.Services
{
	internal abstract class OEmbedServiceBase
	{
        protected static class OEmbedMediaTypeNames
        {
            private const string suffix = "+oembed";

            public const string TextXml = MediaTypeNames.Text.Xml + suffix;
            public const string ApplicationJson = MediaTypeNames.Application.Json + suffix;
        }

        protected static bool IsJson(string mediaType)
		{
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                return false;
            }
            return mediaType.Equals(MediaTypeNames.Application.Json, StringComparison.InvariantCultureIgnoreCase);
        }

        protected static bool IsXml(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                return false;
            }

            return mediaType.Equals(MediaTypeNames.Text.Xml, StringComparison.InvariantCultureIgnoreCase) ||
                   mediaType.Equals(MediaTypeNames.Application.Xml, StringComparison.InvariantCultureIgnoreCase);
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

            return IsJson(mediaType.Substring(0, indexOfSuffix)) ||
                IsXml(mediaType.Substring(0, indexOfSuffix));
        }

		protected static bool IsAlternateOrAlternative(string rel)
        {
            return rel == "alternate" || rel == "alternative";
        }

	}
}