namespace Forma.Tests.Resources;

using System.Diagnostics.CodeAnalysis;
using System.Text;

using Forma.Framework.Resources;

/// <summary>Serves content held in memory, keyed by the address a test asks for.</summary>
public sealed class FakeResourceContentReader : IResourceContentReader
{
    private readonly Dictionary<string, byte[]> _content = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether a stream this reader handed out is still open.</summary>
    public bool HasOpenStream { get; private set; }

    /// <summary>Whether streams this reader hands out refuse to seek, as a network response would.</summary>
    public bool Seekable { get; init; } = true;

    /// <summary>Holds <paramref name="text"/> as UTF-8 bytes behind <paramref name="source"/>.</summary>
    public FakeResourceContentReader Add(string source, string text)
        => Add(source, Encoding.UTF8.GetBytes(text));

    /// <summary>Holds <paramref name="content"/> as the bytes behind <paramref name="source"/>.</summary>
    public FakeResourceContentReader Add(string source, byte[] content)
    {
        _content[source] = content;

        return this;
    }

    /// <inheritdoc />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public ValueTask<Stream?> OpenAsync(string source, CancellationToken cancellationToken)
    {
        if (!_content.TryGetValue(source, out var bytes))
        {
            return ValueTask.FromResult<Stream?>(null);
        }

        HasOpenStream = true;

        Stream stream = new TrackedMemoryStream(bytes, () => HasOpenStream = false);

        return ValueTask.FromResult<Stream?>(Seekable ? stream : new ForwardOnlyStream(stream));
    }

    private sealed class TrackedMemoryStream(byte[] buffer, Action onDisposed) : MemoryStream(buffer, writable: false)
    {
        protected override void Dispose(bool disposing)
        {
            onDisposed();

            base.Dispose(disposing);
        }
    }

    private sealed class ForwardOnlyStream(Stream inner) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
            => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
            => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
