namespace Forma.Framework.Explorer.Items.Requests;

/// <summary>Why an explorer refused to accept an item.</summary>
public enum ExplorerRejectionReason
{
    /// <summary>The kind is not one the explorer accepts.</summary>
    ResourceKind,

    /// <summary>Adding the folder would exceed the maximum nesting depth.</summary>
    FolderDepth,

    /// <summary>The explorer already contains the maximum number of root folders.</summary>
    FolderRootCount,

    /// <summary>The folder already contains the maximum number of items.</summary>
    FolderItemCount,

    /// <summary>The parent already contains the maximum number of subfolders.</summary>
    SubfolderCount,

    /// <summary>The explorer already contains the maximum number of items.</summary>
    ItemCount,

    /// <summary>The explorer has reached its maximum total size.</summary>
    TotalBytes,
}
