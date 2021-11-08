using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Media;

namespace Novicell.Umbraco.OEmbed.Core.Services
{
    internal class OEmbedDiscoveryService : OEmbedServiceBase, IOEmbedDiscoveryService
    {
        private const string TypeAttributeName = "type";
        private const string RelAttributeName = "rel";

        private readonly IHttpClientFactory _httpClientFactory;

        public OEmbedDiscoveryService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

            Uri endpoint;

            try
            {
                var response = await client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                endpoint = FindOEmbedEndpointInHtml(content);

                if(endpoint == null && response.Headers.TryGetValues("Link", out var link))
                {
                    endpoint = FindOEmbedEndpointInLinkHeader(link);
                }

            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return Attempt.Fail<IEmbedProvider>(null);
                }

                return Attempt.Fail<IEmbedProvider>(null, new Exception($"Error downloading html from '{url}'", e));
            }
            catch (Exception e)
            {
                return Attempt.Fail<IEmbedProvider>(null, e);
            }

            if (endpoint != null)
            {
                if (!endpoint.IsAbsoluteUri)
                {
                    endpoint = new Uri(url, endpoint);
                }

                var provider = new AutodiscoverEmbedProvider(endpoint);

                return Attempt.Succeed((IEmbedProvider)provider);
            }

            return Attempt.Fail<IEmbedProvider>(null);
        }

        /// <summary>
        /// Find the endpoint defined in a link-header.
        /// </summary>
        /// <param name="linkHeaderValues">The link-header values from the page being disvored.</param>
        /// <returns>The endpoint found in one of the provided link-header values.</returns>
        internal static Uri FindOEmbedEndpointInLinkHeader(IEnumerable<string> linkHeaderValues)
        {
            var links = new List<(string Href, string MediaType)>();

            foreach (var headerValue in linkHeaderValues)
            {
                var inputs = headerValue.Split(new[] { ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (!inputs.Any())
                {
                    continue;
                }

                var url = inputs[0].StartsWith('<') && inputs[0].EndsWith('>') ?
                    inputs[0][1..^1] : null;

                if (url == null)
                {
                    continue;
                }

                if (!inputs.Skip(1)
                    .Where(x => x.StartsWith(RelAttributeName))
                    .Any(x => IsAlternateOrAlternative(x[(RelAttributeName.Length + 1)..])))
                {
                    continue;
                }

                var type = inputs.Skip(1)
                    .Where(x => x.StartsWith(TypeAttributeName))
                    .Select(x => x[(TypeAttributeName.Length + 1)..])
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                if (type == null)
                {
                    continue;
                }

                links.Add((url, type));
            }

            return GetOEmbedEndpointFromLinks(links.ToArray());
        }

        /// <summary>
        /// Find the endpoint defined in a link-element found in the html.
        /// </summary>
        /// <param name="html">The html content of the page being discovered.</param>
        /// <returns>The endpoint found in a link-element in the html.</returns>
        internal static Uri FindOEmbedEndpointInHtml(string html)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();

            doc.LoadHtml(html);

            var links = doc.DocumentNode.Descendants()
                .Where(e => e.NodeType == HtmlAgilityPack.HtmlNodeType.Element && e.Name == "link")
                .Select(x => new
                {
                    Href = HttpUtility.HtmlDecode(x.GetAttributeValue("href", string.Empty)),
                    Rel = HttpUtility.HtmlDecode(x.GetAttributeValue(RelAttributeName, string.Empty)),
                    Type = HttpUtility.HtmlDecode(x.GetAttributeValue(TypeAttributeName, string.Empty)),
                })
                .Where(x => IsAlternateOrAlternative(x.Rel?.ToLowerInvariant()))
                .Where(x => !string.IsNullOrWhiteSpace(x.Href))
                .ToList();

            return GetOEmbedEndpointFromLinks(links.Select(x => (x.Href, x.Type)).ToArray());
        }

        private static Uri GetOEmbedEndpointFromLinks(ICollection<(string Href, string MediaType)> links)
        {
            if (!links.Any())
            {
                return null;
            }

            foreach (var p in new Func<string, bool>[] { 
                   t => IsJson(t, OEmbedMediaTypeSuffix),
                   t => IsXml(t, OEmbedMediaTypeSuffix), })
            {
                foreach(var (href, _) in links.Where(x => p(x.MediaType)))
                {
                    if (Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var _url))
                    {
                        return _url;
                    }
                }
            }

            return null;
        }

        internal sealed class AutodiscoverEmbedProvider : IEmbedProvider
        {
            public AutodiscoverEmbedProvider(Uri endpoint)
            {
                if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

                ApiEndpoint = endpoint.GetLeftPart(UriPartial.Path);

                RequestParams = GetRequestParameters(endpoint.Query);
            }

            private static Dictionary<string, string> GetRequestParameters(string query)
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