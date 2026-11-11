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
            { Resource: { } resource } => resource.GetKind(),
            _ => CatalogItemKind.Unknown
        };
    }

    /// <summary>The kind of thing the resource is, as a catalog item.</summary>
    /// <param name="resource">The resource.</param>
    public static CatalogItemKind GetKind(this ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource switch
        {
            FolderResourceInfo => CatalogItemKind.Folder,
            ImageResourceInfo => CatalogItemKind.Image,
            VideoResourceInfo => CatalogItemKind.Video,
            AudioResourceInfo => CatalogItemKind.Audio,
            ArchiveResourceInfo => CatalogItemKind.Archive,
            DocumentResourceInfo => CatalogItemKind.Document,
            FileResourceInfo => CatalogItemKind.File,
            _ => CatalogItemKind.Unknown
        };
    }
}
