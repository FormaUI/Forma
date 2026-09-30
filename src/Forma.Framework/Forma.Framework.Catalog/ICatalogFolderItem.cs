namespace Forma.Framework.Catalog;

using Forma.Framework.Resources;

/// <summary>A catalog item representing a folder.</summary>
public interface ICatalogFolderItem : ICatalogItem
{
    /// <summary>The key of the folder's parent folder, where null is the top level.</summary>
    string? ParentKey { get; }

    /// <summary>The number of items in the folder.</summary>
    int? Count { get; }

    /// <summary>The folder resource the item wraps.</summary>
    new FolderResourceInfo? Resource { get; }
}
