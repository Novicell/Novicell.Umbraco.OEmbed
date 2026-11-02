using System.Net;
using System.Net.Sockets;

namespace Novicell.Umbraco.OEmbed.Services;

/// <summary>
/// Connects only to publicly routable addresses, so an editor-supplied URL (or a redirect, or an
/// endpoint advertised by a page) cannot make the server call its own loopback interface, the
/// internal network, or a cloud metadata service.
/// </summary>
/// <remarks>
/// The check happens at connect time, on the address that is actually connected to. Checking the
/// host name before the request would be bypassed by a DNS name that resolves differently the
/// second time (DNS rebinding), and by redirects.
/// </remarks>
internal static class PublicNetworkConnector
{
    internal const string BlockedMessage =
        "The host does not resolve to a public network address. Set Novicell:OEmbed:AllowPrivateNetworks " +
        "to true to allow requests to loopback and private networks.";

    internal static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        DnsEndPoint endpoint = context.DnsEndPoint;

        IPAddress[] addresses = await Dns.GetHostAddressesAsync(endpoint.Host, cancellationToken);
        IPAddress[] publicAddresses = addresses.Where(IpAddressClassifier.IsPublic).ToArray();

        if (publicAddresses.Length == 0)
        {
            throw new HttpRequestException(BlockedMessage);
        }

        Exception? lastFailure = null;

        foreach (IPAddress address in publicAddresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

            try
            {
                // Connect to the address that was checked, not to the name again.
                await socket.ConnectAsync(new IPEndPoint(address, endpoint.Port), cancellationToken);

                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception e)
            {
                socket.Dispose();
                lastFailure = e;
            }
        }

        throw new HttpRequestException(
            $"Could not connect to {endpoint.Host}:{endpoint.Port}.",
            lastFailure);
    }
}

/// <summary>Decides whether an IP address is a public, routable address.</summary>
internal static class IpAddressClassifier
{
    internal static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        var bytes = address.GetAddressBytes();

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicIPv4(bytes),
            AddressFamily.InterNetworkV6 => IsPublicIPv6(address, bytes),
            _ => false,
        };
    }

    private static bool IsPublicIPv4(byte[] b)
        => !(b[0] == 0                                  // 0.0.0.0/8 "this network"
             || b[0] == 10                              // 10.0.0.0/8 private
             || (b[0] == 100 && (b[1] & 0xC0) == 64)    // 100.64.0.0/10 carrier-grade NAT
             || b[0] == 127                             // 127.0.0.0/8 loopback
             || (b[0] == 169 && b[1] == 254)            // 169.254.0.0/16 link-local, cloud metadata
             || (b[0] == 172 && (b[1] & 0xF0) == 16)    // 172.16.0.0/12 private
             || (b[0] == 192 && b[1] == 0 && b[2] == 0) // 192.0.0.0/24 IETF protocol assignments
             || (b[0] == 192 && b[1] == 168)            // 192.168.0.0/16 private
             || (b[0] == 192 && b[1] == 0 && b[2] == 2) // 192.0.2.0/24 documentation (TEST-NET-1)
             || (b[0] == 198 && b[1] == 51 && b[2] == 100) // 198.51.100.0/24 documentation (TEST-NET-2)
             || (b[0] == 203 && b[1] == 0 && b[2] == 113)  // 203.0.113.0/24 documentation (TEST-NET-3)
             || (b[0] == 198 && (b[1] & 0xFE) == 18)    // 198.18.0.0/15 benchmarking
             || b[0] >= 224);                           // 224.0.0.0/4 multicast, 240.0.0.0/4 reserved

    private static bool IsPublicIPv6(IPAddress address, byte[] b)
    {
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast
            || address.IsIPv6UniqueLocal || address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        // 2001:db8::/32 documentation
        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0d && b[3] == 0xb8)
        {
            return false;
        }

        // Ranges that are never public, or that can carry an IPv4 address in a layout that is not fixed.
        // 64:ff9b:1::/48 local-use NAT64 (RFC 8215): the operator chooses how an IPv4 address is embedded.
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b && b[4] == 0x00 && b[5] == 0x01)
        {
            return false;
        }

        // 100::/64 discard-only
        if (b[0] == 0x01 && b[1] == 0x00 && b[2..8].All(x => x == 0))
        {
            return false;
        }

        // 2001::/23 IETF protocol assignments: Teredo, benchmarking, ORCHID and similar
        if (b[0] == 0x20 && b[1] == 0x01 && (b[2] & 0xFE) == 0)
        {
            return false;
        }

        // 3fff::/20 documentation
        if (b[0] == 0x3f && b[1] == 0xff && (b[2] & 0xF0) == 0)
        {
            return false;
        }

        // 5f00::/16 segment routing SIDs
        if (b[0] == 0x5f && b[1] == 0x00)
        {
            return false;
        }

        // Addresses that embed an IPv4 address are only as public as that IPv4 address.
        // ::a.b.c.d (IPv4-compatible)
        if (b.Take(12).All(x => x == 0))
        {
            return IsPublicIPv4(b[12..]);
        }

        // 64:ff9b::/96 (NAT64)
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b && b[4..12].All(x => x == 0))
        {
            return IsPublicIPv4(b[12..]);
        }

        // 2002::/16 (6to4): the IPv4 address is in bytes 2-5
        if (b[0] == 0x20 && b[1] == 0x02)
        {
            return IsPublicIPv4(b[2..6]);
        }

        return true;
    }
}
