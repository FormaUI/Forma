namespace Forma.Tests.Interop;

using Microsoft.JSInterop;

/// <summary>A JS stream reference whose stream opens once it is released, as a long text's chunks are still on their way.</summary>
/// <param name="bytes">The bytes the stream carries.</param>
public sealed class HeldJSStreamReference(byte[] bytes) : IJSStreamReference
{
    // Plain, so the reader's continuation runs where the test releases it.
    private readonly TaskCompletionSource _release = new();

    /// <inheritdoc />
    public long Length
        => bytes.Length;

    /// <summary>Lets the stream open.</summary>
    public void Release()
        => _release.TrySetResult();

    /// <inheritdoc />
    public async ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new MemoryStream(bytes, writable: false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}
