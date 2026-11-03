namespace Forma.Tests.Interop;

using Microsoft.JSInterop;

/// <summary>A JS stream reference over bytes held in memory, or over a stream that fails as it is read.</summary>
/// <remarks>Its stream reads through the reference, as WebAssembly's pulls each chunk through it, so a read after the reference's release fails.</remarks>
/// <param name="bytes">The bytes the stream carries.</param>
/// <param name="failure">The exception reading the stream throws, where it breaks on its way.</param>
/// <param name="length">The length the stream declares, where it declares one other than its bytes'.</param>
public sealed class FakeJSStreamReference(byte[] bytes, Exception? failure = null, long? length = null) : IJSStreamReference
{
    /// <summary>The size the last open accepted.</summary>
    public long? OpenedWith { get; private set; }

    /// <summary>Whether the reference has been released.</summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public long Length
        => length ?? bytes.Length;

    /// <inheritdoc />
    public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        OpenedWith = maxAllowedSize;

        return new ValueTask<Stream>(CreateStream());
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        IsDisposed = true;

        return ValueTask.CompletedTask;
    }

    private PullingStream CreateStream()
        => new(this, bytes, failure);

    private sealed class PullingStream(FakeJSStreamReference reference, byte[] bytes, Exception? failure) : Stream
    {
        private readonly MemoryStream _source = new(bytes);

        public override bool CanRead
            => true;

        public override bool CanSeek
            => false;

        public override bool CanWrite
            => false;

        public override long Length
            => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfUnreadable();

            return _source.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfUnreadable();

            return _source.ReadAsync(buffer, cancellationToken);
        }

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
                _source.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ThrowIfUnreadable()
        {
            if (reference.IsDisposed)
            {
                throw new JSException("The stream was read after its reference was released.");
            }

            if (failure is not null)
            {
                throw failure;
            }
        }
    }
}
