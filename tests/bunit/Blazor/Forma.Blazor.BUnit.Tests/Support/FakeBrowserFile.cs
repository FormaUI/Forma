namespace Forma.Blazor.BUnit.Tests.Support;

using System.Text;

using Microsoft.AspNetCore.Components.Forms;

/// <summary>A browser file backed by an in-memory string.</summary>
public sealed class FakeBrowserFile(string name, string content, string contentType = "text/plain") : IBrowserFile
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
        => _bytes.Length;

    /// <inheritdoc />
    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        => Size > maxAllowedSize
            ? throw new IOException($"{name} is larger than the {maxAllowedSize} byte ceiling.")
            : new MemoryStream(_bytes, writable: false);
}
