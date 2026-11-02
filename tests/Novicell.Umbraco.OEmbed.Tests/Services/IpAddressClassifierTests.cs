using System.Net;
using Novicell.Umbraco.OEmbed.Services;
using NUnit.Framework;

namespace Novicell.Umbraco.OEmbed.Tests.Services;

[TestFixture]
public class IpAddressClassifierTests
{
    // Loopback, private, link-local (cloud metadata), CGNAT, reserved and multicast.
    [TestCase("127.0.0.1")]
    [TestCase("127.1.2.3")]
    [TestCase("0.0.0.0")]
    [TestCase("0.1.2.3")]
    [TestCase("10.0.0.1")]
    [TestCase("10.255.255.255")]
    [TestCase("172.16.0.1")]
    [TestCase("172.31.255.255")]
    [TestCase("192.168.0.1")]
    [TestCase("192.168.255.255")]
    [TestCase("169.254.169.254")]
    [TestCase("169.254.0.1")]
    [TestCase("100.64.0.1")]
    [TestCase("100.127.255.255")]
    [TestCase("192.0.0.8")]
    [TestCase("198.18.0.1")]
    [TestCase("198.19.255.255")]
    [TestCase("224.0.0.1")]
    [TestCase("239.255.255.255")]
    [TestCase("240.0.0.1")]
    [TestCase("192.0.2.1")]
    [TestCase("192.0.2.255")]
    [TestCase("198.51.100.1")]
    [TestCase("198.51.100.254")]
    [TestCase("203.0.113.1")]
    [TestCase("203.0.113.254")]
    [TestCase("255.255.255.255")]
    public void Rejects_Private_And_Reserved_IPv4(string address)
        => Assert.That(IpAddressClassifier.IsPublic(IPAddress.Parse(address)), Is.False);

    [TestCase("::")]
    [TestCase("::1")]
    [TestCase("fe80::1")]
    [TestCase("fc00::1")]
    [TestCase("fd12:3456:789a::1")]
    [TestCase("fec0::1")]
    [TestCase("ff02::1")]
    [TestCase("64:ff9b:1::a9fe:a9fe")]
    [TestCase("64:ff9b:1::808:808")]
    [TestCase("64:ff9b:1:1234::1")]
    [TestCase("100::1")]
    [TestCase("2001::1")]
    [TestCase("2001:2::1")]
    [TestCase("2001:1ff::1")]
    [TestCase("3fff::1")]
    [TestCase("3fff:fff::1")]
    [TestCase("5f00::1")]
    [TestCase("2001:db8::1")]
    public void Rejects_Private_And_Reserved_IPv6(string address)
        => Assert.That(IpAddressClassifier.IsPublic(IPAddress.Parse(address)), Is.False);

    // The IPv4 address inside these decides.
    [TestCase("::ffff:127.0.0.1")]
    [TestCase("::ffff:10.0.0.1")]
    [TestCase("::ffff:169.254.169.254")]
    [TestCase("64:ff9b::7f00:1")]
    [TestCase("64:ff9b::a9fe:a9fe")]
    [TestCase("2002:7f00:1::1")]
    [TestCase("2002:c0a8:101::1")]
    [TestCase("::ffff:192.0.2.1")]
    [TestCase("::ffff:198.51.100.7")]
    [TestCase("64:ff9b::c000:201")]
    [TestCase("2002:cb00:7101::1")]
    [TestCase("::127.0.0.1")]
    public void Rejects_Ipv6_Addresses_That_Embed_A_Private_IPv4(string address)
        => Assert.That(IpAddressClassifier.IsPublic(IPAddress.Parse(address)), Is.False);

    // Just outside each private range, and well-known public addresses.
    [TestCase("8.8.8.8")]
    [TestCase("1.1.1.1")]
    [TestCase("93.184.216.34")]
    [TestCase("172.15.255.255")]
    [TestCase("172.32.0.1")]
    [TestCase("192.167.255.255")]
    [TestCase("192.169.0.1")]
    [TestCase("100.63.255.255")]
    [TestCase("100.128.0.1")]
    [TestCase("198.17.255.255")]
    [TestCase("198.20.0.1")]
    [TestCase("169.253.255.255")]
    [TestCase("169.255.0.1")]
    [TestCase("11.0.0.1")]
    [TestCase("192.0.3.1")]
    [TestCase("192.0.1.255")]
    [TestCase("198.51.99.255")]
    [TestCase("198.51.101.1")]
    [TestCase("203.0.112.255")]
    [TestCase("203.0.114.1")]
    [TestCase("223.255.255.255")]
    public void Accepts_Public_IPv4(string address)
        => Assert.That(IpAddressClassifier.IsPublic(IPAddress.Parse(address)), Is.True);

    [TestCase("2606:4700:4700::1111")]
    [TestCase("2001:4860:4860::8888")]
    [TestCase("2a00:1450:4001:81c::200e")]
    [TestCase("::ffff:8.8.8.8")]
    [TestCase("64:ff9b::808:808")]
    [TestCase("2001:200::1")]
    [TestCase("3fff:1000::1")]
    [TestCase("5f01::1")]
    [TestCase("101::1")]
    [TestCase("2002:808:808::1")]
    public void Accepts_Public_IPv6_And_Embedded_Public_IPv4(string address)
        => Assert.That(IpAddressClassifier.IsPublic(IPAddress.Parse(address)), Is.True);
}
