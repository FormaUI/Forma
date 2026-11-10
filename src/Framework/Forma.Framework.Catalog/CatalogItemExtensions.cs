namespace Forma.Framework.Catalog;

using Forma.Framework.Resources;

/// <summary>Reads what a catalog item is.</summary>
public static class CatalogItemExtensions
{
    /// <summary>The kind of thing the item is.</summary>
    /// <param name="item">The item.</param>
    public static CatalogItemKind GetKind(this ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item switch
        {
            ICatalogFolderItem => CatalogItemKind.Folder,
            { Resource: ImageResourceInfo } => CatalogItemKind.Image,
            { Resource: VideoResourceInfo } => CatalogItemKind.Video,
            { Resource: AudioResourceInfo } => CatalogItemKind.Audio,
            { Resource: ArchiveResourceInfo } => CatalogItemKind.Archive,
            { Resource: DocumentResourceInfo } => CatalogItemKind.Document,
            { Resource: FileResourceInfo } => CatalogItemKind.File,
            _ => CatalogItemKind.Unknown
        };
    }
}
