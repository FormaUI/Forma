namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>Resolves the addresses a browser can render unaided, by the media type their extension implies.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public sealed class BrowserResourceProvider : IResourceProvider
{
    /// <inheritdoc />
    public ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
        => ValueTask.FromResult<ResourceInfo?>(Resolve(source));

    private static FileResourceInfo? Resolve(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var extension = new FileResourceInfo(source).Extension;

        return BrowserContentTypes.TryGetByExtension(extension, out var contentType)
            ? Create(source, contentType)
            : null;
    }

    private static FileResourceInfo Create(string source, string contentType)
    {
        FileResourceInfo resource = contentType switch
        {
            var type when type.StartsWith("image/", StringComparison.OrdinalIgnoreCase) => new ImageResourceInfo(source),
            var type when type.StartsWith("video/", StringComparison.OrdinalIgnoreCase) => new VideoResourceInfo(source),
            var type when type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) => new AudioResourceInfo(source),
            var type when type.StartsWith("text/", StringComparison.OrdinalIgnoreCase) => new DocumentResourceInfo(source),
            "application/pdf" => new DocumentResourceInfo(source),
            _ => new FileResourceInfo(source),
        };

        return resource with { ContentType = contentType };
    }
}
