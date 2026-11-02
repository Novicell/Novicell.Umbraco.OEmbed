using System.Collections.Specialized;
using System.Net;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core;

namespace Novicell.Umbraco.OEmbed.Services;

internal class OEmbedService(
    IOEmbedApiEndpointProvider endpointProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<OEmbedService> logger)
    : IOEmbedService
{
    public async Task<Attempt<OEmbedResponse?, OEmbedOperationStatus>> GetOEmbedAsync(
        OEmbedRequest request,
        CancellationToken cancellationToken)
    {
        // Only http(s) URLs are looked up, and nothing else is ever requested on an editor's behalf.
        if (!OEmbedHttp.IsHttpUrl(request.Url))
        {
            return Attempt<OEmbedResponse?, OEmbedOperationStatus>.Fail(OEmbedOperationStatus.ProviderNotSupported);
        }

        OEmbedApiEndpoint? apiEndpoint;
        try
        {
            apiEndpoint = await endpointProvider.GetApiEndpointAsync(request.Url);
        }
        catch (Exception e)
        {
            // A provider with a bad ApiEndpoint, or a UrlSchemeRegex that times out. That is the site's
            // configuration, so it is logged, and the editor is told the URL is not supported.
            logger.LogError(e, "Finding the oEmbed endpoint for {Url} failed", request.Url);
            return Attempt<OEmbedResponse?, OEmbedOperationStatus>.Fail(OEmbedOperationStatus.ProviderNotSupported);
        }

        if (apiEndpoint == null || !OEmbedHttp.IsHttpUrl(apiEndpoint.Endpoint))
        {
            return Attempt<OEmbedResponse?, OEmbedOperationStatus>.Fail(OEmbedOperationStatus.ProviderNotSupported);
        }

        using HttpClient client = httpClientFactory.CreateClient(OEmbedHttp.ClientName);

        // One deadline for the request and for reading the body, see OEmbedHttp.CreateDeadline.
        using CancellationTokenSource deadline = OEmbedHttp.CreateDeadline(cancellationToken);

        try
        {
            Uri endpointUrl = BuildEndpointUrl(apiEndpoint.Endpoint, request);
            return await FetchOEmbedAsync(endpointUrl, client, deadline.Token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "OEmbed request to {Provider} failed", apiEndpoint.Name);
            return Attempt<OEmbedResponse?, OEmbedOperationStatus>.Fail(OEmbedOperationStatus.InvalidResponse);
        }
    }

    private static Uri BuildEndpointUrl(Uri apiEndpoint, OEmbedRequest request)
    {
        var builder = new UriBuilder(apiEndpoint);

        NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
        query.Set("url", request.Url.ToString());
        // Only JSON is read. Some providers answer with XML unless they are asked for JSON (Flickr).
        query.Set("format", "json");
        if (request.MaxWidth > 0)
        {
            query.Set("maxwidth", request.MaxWidth.ToString());
        }

        if (request.MaxHeight > 0)
        {
            query.Set("maxheight", request.MaxHeight.ToString());
        }

        builder.Query = query.ToString();

        return builder.Uri;
    }

    private static async Task<Attempt<OEmbedResponse?, OEmbedOperationStatus>> FetchOEmbedAsync(
        Uri url,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // ResponseHeadersRead: the body is not buffered here, it is read with a size limit below.
        using HttpResponseMessage response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => Attempt<OEmbedResponse?, OEmbedOperationStatus>.Fail(OEmbedOperationStatus.EmbedNotFound),
                _ => Attempt<OEmbedResponse?, OEmbedOperationStatus>.Fail(OEmbedOperationStatus.InvalidResponse)
            };
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;

        return OEmbedMediaTypes.IsJson(mediaType)
            ? await FromJsonResponseAsync(response, cancellationToken)
            : Attempt<OEmbedResponse?, OEmbedOperationStatus>.Fail(OEmbedOperationStatus.InvalidResponse);
    }

    private static async Task<Attempt<OEmbedResponse?, OEmbedOperationStatus>> FromJsonResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var json = await OEmbedHttp.ReadLimitedStringAsync(response.Content, cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            return Attempt<OEmbedResponse?, OEmbedOperationStatus>.Fail(OEmbedOperationStatus.InvalidResponse);
        }

        try
        {
            // An unexpected value in one field does not fail the response (it is read as null), and what
            // has no property is kept in OEmbedResponse.Extra. Only a missing or invalid type does.
            return ToAttempt(JsonSerializer.Deserialize<OEmbedResponse>(json));
        }
        catch (JsonException)
        {
            return Attempt<OEmbedResponse?, OEmbedOperationStatus>.Fail(OEmbedOperationStatus.InvalidResponse);
        }
    }

    private static Attempt<OEmbedResponse?, OEmbedOperationStatus> ToAttempt(OEmbedResponse? result)
        => Attempt<OEmbedResponse?, OEmbedOperationStatus>.If(
            result is not null,
            OEmbedOperationStatus.Success,
            OEmbedOperationStatus.InvalidResponse,
            result);
}
