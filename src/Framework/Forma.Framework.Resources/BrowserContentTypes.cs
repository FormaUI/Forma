namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>The media types a browser renders on its own, by the file extension that implies each.</summary>
public static class BrowserContentTypes
{
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

    /// <summary>Gets the media type a browser renders a file of <paramref name="extension"/> as, such as <c>image/jpeg</c> for <c>.jpg</c>.</summary>
    /// <param name="extension">The extension, with its leading dot.</param>
    /// <param name="contentType">The media type.</param>
    /// <returns>Whether a browser renders a file of the extension on its own.</returns>
    public static bool TryGetByExtension(string extension, [NotNullWhen(true)] out string? contentType)
    {
        ArgumentNullException.ThrowIfNull(extension);

        return ContentTypesByExtension.TryGetValue(extension, out contentType);
    }
}
