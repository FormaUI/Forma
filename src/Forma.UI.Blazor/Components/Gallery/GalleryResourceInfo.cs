namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

/// <summary>A gallery entry, carrying the timestamps the gallery sorts and lists by.</summary>
public record GalleryResourceInfo : ResourceInfo
{
    /// <summary>Creates an entry for the given source address.</summary>
    public GalleryResourceInfo(string source, ResourceKind kind = ResourceKind.Unknown)
        : base(source, kind)
    {
    }

    /// <summary>When the gallery took the resource in, when the host knew.</summary>
    public DateTimeOffset? AddedAt { get; init; }

    /// <summary>When the resource last changed, when the host knew.</summary>
    public DateTimeOffset? ModifiedAt { get; init; }
}
