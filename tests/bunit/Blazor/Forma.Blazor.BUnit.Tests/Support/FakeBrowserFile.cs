namespace Forma.Blazor.BUnit.Tests.Support;

using System.Text;

using Microsoft.AspNetCore.Components.Forms;

/// <summary>A browser file backed by an in-memory string, of the size the browser describes it with.</summary>
/// <param name="name">The file's name.</param>
/// <param name="content">The file's content.</param>
/// <param name="contentType">The file's type.</param>
/// <param name="size">The size the browser describes the file with, where null is its content's own.</param>
public sealed class FakeBrowserFile(string name, string content, string contentType = "text/plain", long? size = null) : IBrowserFile
{
    private readonly byte[] _bytes = Encoding.UTF8.GetBytes(content);

    /// <inheritdoc />
    public string ContentType
        => contentType;

    /// <inheritdoc />
    public DateTimeOffset LastModified
        => DateTimeOffset.UnixEpoch;

    /// <inheritdoc />
    public string Name
        => name;

    /// <inheritdoc />
    public long Size
        => size ?? _bytes.Length;

    /// <summary>The ceiling the file was opened for reading with, where null is that it wasn't opened.</summary>
    public long? OpenedWith { get; private set; }

    /// <inheritdoc />
    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        OpenedWith = maxAllowedSize;

        return Size > maxAllowedSize
            ? throw new IOException($"{name} is larger than the {maxAllowedSize} byte ceiling.")
            : new MemoryStream(_bytes, writable: false);
    }
}
