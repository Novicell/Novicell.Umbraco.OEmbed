using System.Net;
using System.Net.Http.Headers;

namespace Novicell.Umbraco.OEmbed.Tests.TestSupport;

/// <summary>
/// Response content that does not declare a length, like a chunked response, so a size limit has to
/// be enforced by counting the bytes that are read.
/// </summary>
internal sealed class UnknownLengthContent : HttpContent
{
    private readonly byte[] _bytes;

    public UnknownLengthContent(byte[] bytes, string mediaType)
    {
        _bytes = bytes;
        Headers.ContentType = new MediaTypeHeaderValue(mediaType);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => stream.WriteAsync(_bytes).AsTask();

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
