namespace Forma.Framework.Catalog;

using Forma.Framework.Resources;

/// <summary>A catalog item representing a folder.</summary>
public interface ICatalogFolderItem : ICatalogItem
{
    /// <summary>The key of the folder this one sits inside, or null when it is top-level.</summary>
    string? ParentKey { get; }

    /// <summary>How many items the folder holds, or null when unknown.</summary>
    int? Count { get; }

    /// <summary>The underlying folder resource, or null when the item does not represent one.</summary>
    new FolderResourceInfo? Resource { get; }
}
