using System.Text;

namespace Novicell.Umbraco.OEmbed.Services;

/// <summary>
/// The limits for HTTP calls made on behalf of an editor, to an oEmbed endpoint. A provider can
/// redirect or answer with anything, so every call gets bounds on time, redirects and response size.
/// </summary>
internal static class OEmbedHttp
{
    /// <summary>The name of the <see cref="HttpClient"/> used for all oEmbed traffic.</summary>
    internal const string ClientName = "Novicell.Umbraco.OEmbed";

    internal const int MaxRedirects = 3;

    internal const long MaxResponseBytes = 1024 * 1024;

    static OEmbedHttp()
    {
        // Lets a response declare a legacy charset such as windows-1252; HttpContent does the same.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// How long a request may take, from sending it until its body has been read. Settable only so
    /// tests do not have to wait for the real value.
    /// </summary>
    internal static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// A token that is cancelled when the caller cancels or <see cref="Timeout"/> has passed. Use it
    /// for the request and for reading the body: <see cref="HttpClient.Timeout"/> stops applying once
    /// the response headers have arrived, so a server could otherwise send them and then stall the
    /// body for as long as it likes.
    /// </summary>
    internal static CancellationTokenSource CreateDeadline(CancellationToken cancellationToken)
    {
        CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Timeout);

        return deadline;
    }

    /// <summary>True for an absolute http or https URI. Anything else is never requested.</summary>
    internal static bool IsHttpUrl(Uri? uri)
        => uri is { IsAbsoluteUri: true }
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// Opens the response body, failing if it is larger than <see cref="MaxResponseBytes"/> or if
    /// <paramref name="deadline"/> is cancelled while reading it. The declared length is checked
    /// first, and the bytes actually read are counted, because a server can omit or misstate the
    /// length.
    /// </summary>
    internal static async Task<Stream> ReadLimitedStreamAsync(HttpContent content, CancellationToken deadline)
    {
        if (content.Headers.ContentLength > MaxResponseBytes)
        {
            throw new ResponseTooLargeException();
        }

        Stream stream = await content.ReadAsStreamAsync(deadline);

        return new LimitedReadStream(stream, MaxResponseBytes, deadline);
    }

    /// <summary>
    /// Reads the response body as text, using the charset the response declares (UTF-8 when it does
    /// not), failing if it is larger than the limit or the deadline passes.
    /// </summary>
    internal static async Task<string> ReadLimitedStringAsync(HttpContent content, CancellationToken deadline)
    {
        await using Stream stream = await ReadLimitedStreamAsync(content, deadline);
        using var reader = new StreamReader(stream, EncodingFor(content), detectEncodingFromByteOrderMarks: true);

        return await reader.ReadToEndAsync(deadline);
    }

    private static Encoding EncodingFor(HttpContent content)
    {
        var charset = content.Headers.ContentType?.CharSet?.Trim().Trim('"');

        if (!string.IsNullOrEmpty(charset))
        {
            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch (ArgumentException)
            {
                // An unknown charset: fall back to UTF-8, like HttpContent.ReadAsStringAsync does.
            }
        }

        return Encoding.UTF8;
    }

    /// <summary>Thrown when a response is larger than <see cref="MaxResponseBytes"/>.</summary>
    internal sealed class ResponseTooLargeException()
        : IOException($"The response is larger than {MaxResponseBytes} bytes.");

    /// <summary>
    /// A read-only stream that throws once more than <c>limit</c> bytes have been read, and that
    /// stops waiting for the server when the deadline passes, whatever token the reader passes.
    /// XmlReader, for one, reads the underlying stream without a token.
    /// </summary>
    private sealed class LimitedReadStream(Stream inner, long limit, CancellationToken deadline) : Stream
    {
        private long _read;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => Count(inner.Read(buffer, offset, count));

        public override int Read(Span<byte> buffer)
            => Count(inner.Read(buffer));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline);

            return Count(await inner.ReadAsync(buffer, linked.Token));
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            await base.DisposeAsync();
        }

        private int Count(int bytesRead)
        {
            _read += bytesRead;

            return _read > limit ? throw new ResponseTooLargeException() : bytesRead;
        }
    }
}
