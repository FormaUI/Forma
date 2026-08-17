namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

/// <summary>Information about a folder or directory in a media gallery.</summary>
/// <param name="Id">Unique identifier of the folder.</param>
/// <param name="Name">Display name of the folder.</param>
/// <param name="ParentId">Identifier of the parent folder, or null for top-level folders.</param>
/// <param name="Path">Path representation of the folder hierarchy.</param>
/// <param name="ItemCount">Count of items contained in the folder.</param>
/// <param name="TotalSize">Cumulative byte size of items in the folder, when known.</param>
/// <param name="ModifiedAt">Last modification timestamp of the folder.</param>
public sealed record GalleryFolderInfo(
    string Id,
    string Name,
    string? ParentId = null,
    string Path = "/",
    int ItemCount = 0,
    long? TotalSize = null,
    DateTimeOffset? ModifiedAt = null)
{
    /// <summary>Formatted display size of the folder contents.</summary>
    public string? DisplaySize
        => TotalSize is { } size ? ResourceFormatter.FromByteCount(size) : null;
}
