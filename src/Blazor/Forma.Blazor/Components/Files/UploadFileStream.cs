namespace Forma.Blazor.Components.Files;

/// <summary>A read-only stream over an inner stream that reports the running count of bytes read, up to a ceiling.</summary>
/// <remarks>Each read asks for one byte past what the ceiling leaves at most, so a stream longer than the ceiling fails the read that would pass it without handing its reader a byte beyond it.</remarks>
internal sealed class UploadFileStream(Stream innerStream, long ceiling, Action<long> onProgress) : Stream
{
    private long _bytesRead;

    /// <inheritdoc />
    public override bool CanRead
        => innerStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek
        => false;

    /// <inheritdoc />
    public override bool CanWrite
        => false;

    /// <inheritdoc />
    public override long Length
        => innerStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _bytesRead;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await innerStream.DisposeAsync();

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
        var bytesRead = await innerStream.ReadAsync(buffer[..GetWindow(buffer.Length)], cancellationToken);

        Advance(bytesRead);

        return bytesRead;
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await innerStream.ReadAsync(buffer.AsMemory(offset, GetWindow(count)), cancellationToken);

        Advance(bytesRead);

        return bytesRead;
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
            innerStream.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Advance(int bytesRead)
    {
        if (bytesRead <= 0)
        {
            return;
        }

        _bytesRead += bytesRead;

        if (_bytesRead > ceiling)
        {
            throw new IOException($"The file is longer than the {ceiling} bytes it is read up to.");
        }

        onProgress(_bytesRead);
    }

    private int GetWindow(int count)
        => (int)Math.Min(count, ceiling - _bytesRead + 1);
}
