using System.Text.Encodings.Web;
using NSubstitute;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Infrastructure.Serialization;

namespace Novicell.Umbraco.OEmbed.Tests.TestSupport;

/// <summary>
/// Builds a real (non-mocked) <see cref="IJsonSerializer"/>, matching the concrete implementation
/// Umbraco itself registers via DI, for tests that exercise <c>TypedJsonValidatorRunner</c> end to end.
/// </summary>
internal static class TestJsonSerializer
{
    public static IJsonSerializer Create()
    {
        var encoderFactory = Substitute.For<IJsonSerializerEncoderFactory>();
        encoderFactory.CreateEncoder<SystemTextJsonSerializer>().Returns(JavaScriptEncoder.Default);
        return new SystemTextJsonSerializer(encoderFactory);
    }
}
