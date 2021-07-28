﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Novicell.Umbraco.OEmbed.Configuration.Models;
using Novicell.Umbraco.OEmbed.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Media.EmbedProviders;

namespace Novicell.Umbraco.OEmbed.Media
{
    internal class OEmbedService : OEmbedServiceBase, IOEmbedService
    {
        private readonly EmbedProvidersCollection _providers;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOEmbedDiscoveryService _discoveryService;
        private readonly OEmbedSettings _settings;

        public OEmbedService(
            EmbedProvidersCollection providers,
            IHttpClientFactory httpClientFactory,
            IOEmbedDiscoveryService discoveryService,
            IOptions<OEmbedSettings> settings)
        {
            _providers = providers;
            _httpClientFactory = httpClientFactory;
            _discoveryService = discoveryService;
            _settings = settings?.Value;
        }

        public async Task<Attempt<Models.OEmbedResponse>> GetOEmbedAsync(Uri url, int maxwidth, int maxheight)
        {
            var _url = await GetOEmbedUrlAsync(url, maxwidth, maxheight);

            if (_url == null)
            {
                return Attempt.Fail<Models.OEmbedResponse>(null, new OEmbedUrlNotSupportedException());
            }

            var oembed = await FetchOEmbedFromUrlAsync(_url);

            if (oembed.Success)
            {
                return Attempt.Succeed(oembed.Result);
            }

            if (oembed.Exception != null)
            {
                return Attempt.Fail<Models.OEmbedResponse>(
                    null, new OEmbedException($"Error fetching OEmbed from '{_url}'.", oembed.Exception));
            }

            return Attempt.Fail<Models.OEmbedResponse>();
        }

        private async Task<Uri> GetOEmbedUrlAsync(Uri url, int maxwidth, int maxheight)
        {
            var isUrlMatch = new Func<string, string[], bool>((url, patterns)
                => patterns.Any(p => Regex.IsMatch(url, p, RegexOptions.IgnoreCase)));

            var provider = _providers?
                .Where(x => x.UrlSchemeRegex != null)
                .FirstOrDefault(x => isUrlMatch(url.AbsoluteUri, x.UrlSchemeRegex));

            if (provider == null && _settings?.Autodiscover == true)
            {
                var discovered = await _discoveryService.DiscoverFromUrlAsync(url);

                if (discovered.Success)
                {
                    provider = discovered.Result;
                }
            }

            switch (provider)
            {
                case null:
                    return null;

                case EmbedProviderBase providerBase:
                {
                    var providerUrl = providerBase.GetEmbedProviderUrl(url.ToString(), maxwidth, maxheight);

                    return string.IsNullOrWhiteSpace(providerUrl) ? null : new Uri(providerUrl);
                }
                default:
                    return BuildProviderOEmbedUrl(provider.ApiEndpoint, provider.RequestParams, url, maxwidth, maxheight);
            }
        }

        private async Task<Attempt<Models.OEmbedResponse>> FetchOEmbedFromUrlAsync(Uri url)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(url);

            if(response.StatusCode == HttpStatusCode.NotFound)
            {
                return Attempt.Fail<Models.OEmbedResponse>(null);
            }

            try 
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                return Attempt<Models.OEmbedResponse>.Fail(e);
            }

            var content = await response.Content.ReadAsStringAsync();

            var mediaType = response.Content.Headers.ContentType?.MediaType;

            var json = IsJson(mediaType) ? content :
                IsXml(mediaType) ? ConvertXmlToJson(content) : null;

            if (json == null)
            {
                return Attempt<Models.OEmbedResponse>.Fail(new Exception($"Invalid response - content type '{mediaType??"unkown"}' not supported"));
            }

            var result = JsonConvert.DeserializeObject<Models.OEmbedResponse>(json, new JsonSerializerSettings
            {
                //MissingMemberHandling = MissingMemberHandling.Ignore,
                Error = (sender, args) =>
                {
                    args.ErrorContext.Handled = true;
                },
            });

            return Attempt.Succeed(result);
        }

        private static Uri BuildProviderOEmbedUrl(string endpoint, IDictionary<string,string> parameters, Uri url, int maxwidth, int maxheight)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            var builder = new UriBuilder(endpoint);

            var queryString = HttpUtility.ParseQueryString(builder.Query);

            if (parameters != null)
            {
                foreach (var (key, value) in parameters)
                {
                    queryString[key] = HttpUtility.UrlEncode(value);
                }
            }

            queryString[nameof(url)] = url.ToString();

            if (maxwidth > 0)
            {
                queryString[nameof(maxwidth)] = maxwidth.ToString();
            }

            if (maxheight > 0)
            {
                queryString[nameof(maxheight)] = maxheight.ToString();
            }

            builder.Query = queryString.ToString() ?? string.Empty;

            return builder.Uri;
        }

        private static string ConvertXmlToJson(string xml)
        {
            var x = XDocument.Parse(xml);

            if (x.Root == null)
            {
                return null;
            }

            var o = new JObject();
            foreach (var e in x.Root.Elements())
            {
                var propertyName = new string(e.Name.LocalName
                            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                            .ToArray());

                o[propertyName] = (e.Value);
            }

            return o.ToString(Formatting.None);
        }
	}
}
