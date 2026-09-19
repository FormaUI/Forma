namespace Forma.Framework.Catalog.Requests;

/// <summary>Why a catalog refused to accept an item.</summary>
public enum CatalogRejectionReason
{
    /// <summary>The kind is not one the catalog accepts.</summary>
    ResourceKind,

    /// <summary>Adding the folder would exceed the maximum nesting depth.</summary>
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
}
