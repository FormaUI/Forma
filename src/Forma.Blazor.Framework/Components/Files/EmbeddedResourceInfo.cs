namespace Forma.Blazor.Components.Files;

using System.Diagnostics.CodeAnalysis;

/// <summary>A resource whose media type is known, which is what decides how a browser embeds it.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record EmbeddedResourceInfo : ResourceInfo
{
    public EmbeddedResourceInfo(string source, string contentType)
        : base(source, KindFrom(contentType))
    {
        ContentType = contentType;
    }

    /// <summary>The resource's media type, as a browser would be told it.</summary>
    public string ContentType { get; }

    private static ResourceKind KindFrom(string contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        return contentType switch
        {
            var type when type.StartsWith("image/", StringComparison.OrdinalIgnoreCase) => ResourceKind.Image,
            var type when type.StartsWith("video/", StringComparison.OrdinalIgnoreCase) => ResourceKind.Video,
            var type when type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) => ResourceKind.Audio,
            var type when type.StartsWith("text/", StringComparison.OrdinalIgnoreCase) => ResourceKind.Document,
            var type when type.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) => ResourceKind.Document,
            _ => ResourceKind.Unknown,
        };
    }
}
