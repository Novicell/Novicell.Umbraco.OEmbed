using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Novicell.Umbraco.OEmbed.Configuration;
using Novicell.Umbraco.OEmbed.Models;
using Novicell.Umbraco.OEmbed.Services;
using NUnit.Framework;

namespace Novicell.Umbraco.OEmbed.Tests.Services;

[TestFixture]
public class OEmbedHttpTests
{
    [TestCase("https://example.com/a", true)]
    [TestCase("http://example.com/a", true)]
    [TestCase("HTTPS://EXAMPLE.COM", true)]
    [TestCase("ftp://example.com/a", false)]
    [TestCase("file:///etc/passwd", false)]
    [TestCase("javascript:alert(1)", false)]
    [TestCase("data:text/html,x", false)]
    [TestCase("gopher://example.com", false)]
    public void IsHttpUrl_Accepts_Only_Absolute_Http_And_Https(string url, bool expected)
        => Assert.That(OEmbedHttp.IsHttpUrl(new Uri(url)), Is.EqualTo(expected));

    [Test]
    public void IsHttpUrl_Rejects_Relative_Uris_And_Null()
    {
        Assert.That(OEmbedHttp.IsHttpUrl(new Uri("/relative", UriKind.Relative)), Is.False);
        Assert.That(OEmbedHttp.IsHttpUrl(null), Is.False);
    }

    private static HttpClient CreateClient(bool allowPrivateNetworks = true)
    {
        var services = new ServiceCollection();
        services.Configure<OEmbedOptions>(options => options.AllowPrivateNetworks = allowPrivateNetworks);
        ServiceProvider provider = services.AddOEmbedHttpClient().BuildServiceProvider();
        return provider.GetRequiredService<IHttpClientFactory>().CreateClient(OEmbedHttp.ClientName);
    }

    [Test]
    public void The_Client_Has_A_Timeout_And_A_User_Agent()
    {
        using HttpClient client = CreateClient();

        Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(10)));
        Assert.That(client.DefaultRequestHeaders.UserAgent, Is.Not.Empty);
    }

    [TestCase(3, HttpStatusCode.OK)]
    [TestCase(4, HttpStatusCode.Redirect)]
    public async Task The_Client_Follows_At_Most_Three_Redirects(int hops, HttpStatusCode expected)
    {
        using var server = new RedirectServer();
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync(server.UrlFor(hops));

        Assert.That(response.StatusCode, Is.EqualTo(expected));
    }

    [Test]
    public void Loopback_Requests_Are_Blocked_By_Default()
    {
        using var server = new RedirectServer();
        using HttpClient client = CreateClient(allowPrivateNetworks: false);

        HttpRequestException? exception = Assert.ThrowsAsync<HttpRequestException>(
            async () => await client.GetAsync(server.UrlFor(0)));

        // The block is raised from the connect callback, so it may be wrapped by the handler.
        Assert.That(exception!.ToString(), Does.Contain("AllowPrivateNetworks"));
    }

    [TestCase("http://127.0.0.1:9/")]
    [TestCase("http://[::1]:9/")]
    [TestCase("http://169.254.169.254/latest/meta-data/")]
    [TestCase("http://10.0.0.1/")]
    [TestCase("http://192.168.1.1/")]
    [TestCase("http://[::ffff:127.0.0.1]:9/")]
    public void Private_IP_Literals_Are_Blocked_By_Default(string url)
    {
        using HttpClient client = CreateClient(allowPrivateNetworks: false);

        HttpRequestException? exception = Assert.ThrowsAsync<HttpRequestException>(
            async () => await client.GetAsync(url));

        Assert.That(exception!.ToString(), Does.Contain("AllowPrivateNetworks"));
    }

    [Test]
    public async Task Loopback_Requests_Succeed_When_Private_Networks_Are_Allowed()
    {
        using var server = new RedirectServer();
        using HttpClient client = CreateClient(allowPrivateNetworks: true);

        using HttpResponseMessage response = await client.GetAsync(server.UrlFor(0));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void Without_Private_Networks_The_Handler_Connects_Directly_And_Never_Uses_A_Proxy()
    {
        // A proxy would hide the real destination from the connect check: the check would see the
        // proxy's address, so any public proxy would pass it and forward the request anywhere.
        using SocketsHttpHandler handler = OEmbedHttpClientExtensions.CreateHandler(allowPrivateNetworks: false);

        Assert.That(handler.UseProxy, Is.False);
        Assert.That(handler.ConnectCallback, Is.Not.Null);
        Assert.That(handler.MaxAutomaticRedirections, Is.EqualTo(OEmbedHttp.MaxRedirects));
    }

    [Test]
    public void With_Private_Networks_Allowed_The_Handler_Has_No_Address_Check_And_Honours_The_Proxy_Settings()
    {
        using SocketsHttpHandler handler = OEmbedHttpClientExtensions.CreateHandler(allowPrivateNetworks: true);

        Assert.That(handler.UseProxy, Is.True);
        Assert.That(handler.ConnectCallback, Is.Null);
        Assert.That(handler.MaxAutomaticRedirections, Is.EqualTo(OEmbedHttp.MaxRedirects));
    }

    [Test]
    public void Private_Networks_Are_Not_Allowed_Unless_Configured()
        => Assert.That(new OEmbedOptions().AllowPrivateNetworks, Is.False);

    /// <summary>A loopback server where /hop/N redirects to /hop/N-1 and /hop/0 answers 200.</summary>
    private sealed class RedirectServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly int _port;

        public RedirectServer()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            _port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
            _ = Task.Run(Serve);
        }

        public Uri UrlFor(int hops) => new($"http://localhost:{_port}/hop/{hops}");

        private async Task Serve()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                var hops = int.Parse(context.Request.Url!.Segments[^1]);
                if (hops > 0)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Redirect;
                    context.Response.RedirectLocation = $"/hop/{hops - 1}";
                }

                context.Response.Close();
            }
        }

        public void Dispose() => _listener.Close();
    }
}
