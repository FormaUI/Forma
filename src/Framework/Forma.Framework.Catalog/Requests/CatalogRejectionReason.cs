namespace Forma.Framework.Catalog.Requests;

/// <summary>The reason a catalog rejects an item.</summary>
public enum CatalogRejectionReason
{
    /// <summary>The item's resource kind falls outside the kinds the catalog accepts.</summary>
    ResourceKind,

    /// <summary>The folder exceeds the maximum folder depth.</summary>
    FolderDepth,

    /// <summary>The catalog already holds the maximum number of root folders.</summary>
    FolderRootCount,

    /// <summary>The folder already contains the maximum number of items.</summary>
    FolderItemCount,

    /// <summary>The parent already contains the maximum number of subfolders.</summary>
    SubfolderCount,

    /// <summary>The catalog already holds the maximum number of items.</summary>
    ItemCount,

    /// <summary>The catalog has reached its maximum total size.</summary>
    TotalBytes,

    /// <summary>A file is larger than the maximum bytes of one file.</summary>
    FileBytes,

    /// <summary>An item's capabilities, or what the catalog permits for its kind, leave the operation out.</summary>
    NotPermitted,

    /// <summary>A folder would move into itself or a folder beneath it.</summary>
    FolderCycle,
}
