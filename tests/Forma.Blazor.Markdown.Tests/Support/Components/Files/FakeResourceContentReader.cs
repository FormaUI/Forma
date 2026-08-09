namespace Forma.Blazor.Markdown.Tests.Support.Components.Files;

using System.Diagnostics.CodeAnalysis;
using System.Text;

using Forma.Blazor.Components.Files;

/// <summary>Serves content held in memory, keyed by the address a test asks for.</summary>
public sealed class FakeResourceContentReader : IResourceContentReader
{
    private readonly Dictionary<string, byte[]> _content = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether a stream this reader handed out is still open.</summary>
    public bool HasOpenStream { get; private set; }

    /// <summary>Holds <paramref name="markdown"/> as UTF-8 bytes behind <paramref name="source"/>.</summary>
    public FakeResourceContentReader Add(string source, string markdown)
        => Add(source, Encoding.UTF8.GetBytes(markdown));

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

        return ValueTask.FromResult<Stream?>(new TrackedMemoryStream(bytes, () => HasOpenStream = false));
    }

    private sealed class TrackedMemoryStream(byte[] buffer, Action onDisposed) : MemoryStream(buffer, writable: false)
    {
        protected override void Dispose(bool disposing)
        {
            onDisposed();

            base.Dispose(disposing);
        }
    }
}
