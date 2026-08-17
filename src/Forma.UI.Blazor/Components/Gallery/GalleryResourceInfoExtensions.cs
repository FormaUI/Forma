namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

/// <summary>Reads gallery timestamps from an entry that may not carry them.</summary>
internal static class GalleryResourceInfoExtensions
{
    /// <summary>When the entry was taken in, or null when it is not a gallery entry.</summary>
    internal static DateTimeOffset? Added(this ResourceInfo entry)
        => (entry as GalleryResourceInfo)?.AddedAt;

    /// <summary>When the entry last changed, or null when it is not a gallery entry.</summary>
    internal static DateTimeOffset? Modified(this ResourceInfo entry)
        => (entry as GalleryResourceInfo)?.ModifiedAt;
}
