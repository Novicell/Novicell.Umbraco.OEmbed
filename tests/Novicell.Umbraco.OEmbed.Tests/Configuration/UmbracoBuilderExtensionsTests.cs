using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Novicell.Umbraco.OEmbed.Configuration;
using NUnit.Framework;
using Umbraco.Cms.Core.DependencyInjection;
using OEmbedBuilderExtensions = Novicell.Umbraco.OEmbed.Configuration.UmbracoBuilderExtensions;

namespace Novicell.Umbraco.OEmbed.Tests.Configuration;

[TestFixture]
public class UmbracoBuilderExtensionsTests
{
    [Test]
    public void A_Second_Call_Registers_Nothing_And_Returns_The_Builder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<OEmbedBuilderExtensions.RegistrationMarker>();
        IUmbracoBuilder builder = Substitute.For<IUmbracoBuilder>();
        builder.Services.Returns(services);

        IUmbracoBuilder result = builder.AddNovicellOEmbed();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(builder));
            Assert.That(services, Has.Count.EqualTo(1));
        });
        _ = builder.DidNotReceive().Config;
    }
}
