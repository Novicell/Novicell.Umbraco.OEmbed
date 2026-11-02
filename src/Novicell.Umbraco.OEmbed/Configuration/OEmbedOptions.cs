namespace Novicell.Umbraco.OEmbed.Configuration;

public class OEmbedOptions
{
    internal const string SectionKey = nameof(Novicell) + ":" + nameof(OEmbed);

    /// <summary>
    /// Allows requests to loopback and private network addresses. Off by default: the server then
    /// only connects to publicly routable addresses, so a provider endpoint or a redirect cannot
    /// reach internal services or a cloud metadata endpoint.
    /// </summary>
    /// <remarks>
    /// Turn this on to test with a provider on localhost, or when the server can only reach the
    /// internet through an outbound proxy. With it off, the server connects directly and ignores
    /// proxy settings, because a proxy would hide the real destination from the address check. With
    /// it on, no address check is made and the normal proxy settings apply, so the proxy is
    /// responsible for restricting what can be reached. The setting is read when the HTTP client is
    /// created, so changing it needs a restart.
    /// </remarks>
    public bool AllowPrivateNetworks { get; set; }
}
