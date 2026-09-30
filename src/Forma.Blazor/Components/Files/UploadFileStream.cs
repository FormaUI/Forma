namespace Forma.Blazor.Components.Files;

/// <summary>A read-only stream over an inner stream that reports the running count of bytes read.</summary>
internal sealed class UploadFileStream(Stream inner, Action<long> onProgress) : Stream
{
    private long _bytesRead;

    /// <inheritdoc />
    public override bool CanRead
        => inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek
        => false;

    /// <inheritdoc />
    public override bool CanWrite
        => false;

    /// <inheritdoc />
    public override long Length
        => inner.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _bytesRead;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync();

        await base.DisposeAsync();
    }

    /// <inheritdoc />
    public override void Flush()
        => throw new NotSupportedException();

    /// <inheritdoc />
    // The browser's stream is async-only.
    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken);

        Advance(read);

        return read;
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        Advance(read);

        return read;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Advance(int read)
    {
        if (read <= 0)
        {
            return;
        }

        _bytesRead += read;
        onProgress(_bytesRead);
    }
}
