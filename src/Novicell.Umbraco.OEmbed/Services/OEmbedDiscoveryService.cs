using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Novicell.Umbraco.OEmbed.Media;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Media;

namespace Novicell.Umbraco.OEmbed.Services
{
    internal class OEmbedDiscoveryService : OEmbedServiceBase, IOEmbedDiscoveryService
    {
        private static class OEmbedMediaTypeNames
		{
            private const string suffix = "+oembed";

            public const string TextXml = MediaTypeNames.Text.Xml + suffix;
            public const string ApplicationJson = MediaTypeNames.Application.Json + suffix;
        }


        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OEmbedDiscoveryService> _logger;

        public OEmbedDiscoveryService(
            IHttpClientFactory httpClientFactory,
            ILogger<OEmbedDiscoveryService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Discover the OEmbed endpoint of the provided url.
        /// </summary>
        /// <remarks>
        /// https://oembed.com/#section4
        /// oEmbed providers can choose to make their oEmbed support discoverable by adding
        /// elements to the head of their existing (X)HTML documents.
        /// The URLs contained within the href attribute should be the full oEmbed endpoint
        /// plus URL and any needed format parameter. No other request parameters should be
        /// included in this URL.
        /// The type attribute must contain either application/json+oembed for JSON responses,
        /// or text/xml+oembed for XML.
        /// </remarks>
        /// <param name="url">The url of the (X)HTML document that contains the resource that should be embedded.</param>
        /// <returns>If discovered, an <see cref="IEmbedProvider"/> that describes the endpoint.</returns>
        public async Task<Attempt<IEmbedProvider>> DiscoverFromUrlAsync(Uri url)
        {
            var client = _httpClientFactory.CreateClient();

            var html = new HtmlAgilityPack.HtmlDocument();

            Uri endpoint;

            try
            {
                var response = await client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                html.LoadHtml(content);

                endpoint = FindOEmbedEndpointUrl(html, url);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return Attempt.Fail<IEmbedProvider>(null);
                }

                return Attempt<IEmbedProvider>.Fail(new Exception($"Error downloading html from '{url}'", e));
            }
            catch (Exception e)
            {
                return Attempt<IEmbedProvider>.Fail(e);
            }

            if (endpoint != null)
            {
                var provider = new AutodiscoverEmbedProvider(endpoint);

                return Attempt.Succeed((IEmbedProvider)provider);
            }

            return Attempt.Fail<IEmbedProvider>(null);
        }

        private static Uri FindOEmbedEndpointUrl(HtmlAgilityPack.HtmlDocument html, Uri url)
        {
            var alternateLinks = html.DocumentNode.Descendants()
                .Where(e => e.NodeType == HtmlAgilityPack.HtmlNodeType.Element && e.Name == "link")
                .Select(x => new
                {
                    rel = HttpUtility.HtmlDecode(x.GetAttributeValue("rel", string.Empty))?.ToLowerInvariant(),
                    type = HttpUtility.HtmlDecode(x.GetAttributeValue("type", string.Empty))?.ToLowerInvariant(),
                    href = HttpUtility.HtmlDecode(x.GetAttributeValue("href", string.Empty)),
                })
                .Where(x => IsAlternateOrAlternative(x.rel) && 
                            IsApplicationJsonOrTextXmlWithOEmbedSuffix(x.type) &&
                            !string.IsNullOrWhiteSpace(x.href))
                .Select(x => x.href)
                .ToList();

            if (alternateLinks.Any())
            {
                foreach (var link in alternateLinks)
                {
                    if (!Uri.TryCreate(link, UriKind.RelativeOrAbsolute, out var _url))
                    {
                        continue;
                    }

                    return _url.IsAbsoluteUri ? _url : new Uri(url, _url);
                }
            }

            return null;
        }

        private static bool IsApplicationJsonOrTextXmlWithOEmbedSuffix(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            return type.ToLower() switch
            {
                OEmbedMediaTypeNames.ApplicationJson => true,
                OEmbedMediaTypeNames.TextXml => true,
                _ => false,
            };
        }

        private static bool IsAlternateOrAlternative(string rel)
        {
            return rel == "alternate" || rel == "alternative";
        }

        internal sealed class AutodiscoverEmbedProvider : IEmbedProvider
        {
            public AutodiscoverEmbedProvider(Uri endpoint)
            {
                if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

                ApiEndpoint = endpoint.GetLeftPart(UriPartial.Path);

                RequestParams = GetRequestParameters(endpoint.Query);
            }

            public static Dictionary<string, string> GetRequestParameters(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return null;
                }

                var queryString = HttpUtility.ParseQueryString(query);

                if (queryString.Count > 0)
                {
                    var requestParameters = new Dictionary<string, string>();
                    foreach (var key in queryString.AllKeys)
                    {
                        requestParameters[key!] = queryString.Get(key);
                    }
                    return requestParameters;
                }

                return null;
            }

            public string GetMarkup(string url, int maxWidth = 0, int maxHeight = 0)
            {
                throw new NotImplementedException();
            }

            public string ApiEndpoint { get; }

            public Dictionary<string, string> RequestParams { get; }

            public string[] UrlSchemeRegex => null;
        }
    }
}