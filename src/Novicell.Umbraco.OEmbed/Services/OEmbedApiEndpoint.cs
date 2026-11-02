namespace Novicell.Umbraco.OEmbed.Services;

/// <summary>
/// The oEmbed endpoint of a provider, as found by <see cref="IOEmbedApiEndpointProvider"/>.
/// </summary>
/// <param name="Name">The name of the provider.</param>
/// <param name="Endpoint">The URL of the provider's oEmbed endpoint.</param>
internal record OEmbedApiEndpoint(string Name, Uri Endpoint);
