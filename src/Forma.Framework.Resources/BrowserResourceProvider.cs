namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>Resolves the addresses a browser can render unaided, by the media type their extension implies.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public sealed class BrowserResourceProvider : IResourceProvider
{
    // Only what a browser renders on its own; anything else is left to a converting provider.
    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".apng"] = "image/apng",
        [".avif"] = "image/avif",
        [".bmp"] = "image/bmp",
        [".gif"] = "image/gif",
        [".ico"] = "image/x-icon",
        [".jpeg"] = "image/jpeg",
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".svg"] = "image/svg+xml",
        [".webp"] = "image/webp",
        [".m4a"] = "audio/mp4",
        [".mp3"] = "audio/mpeg",
        [".oga"] = "audio/ogg",
        [".opus"] = "audio/ogg",
        [".wav"] = "audio/wav",
        [".mp4"] = "video/mp4",
        [".ogv"] = "video/ogg",
        [".webm"] = "video/webm",
        [".htm"] = "text/html",
        [".html"] = "text/html",
        [".pdf"] = "application/pdf",
        [".txt"] = "text/plain",
    };

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

        return ContentTypesByExtension.TryGetValue(extension, out var contentType)
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
