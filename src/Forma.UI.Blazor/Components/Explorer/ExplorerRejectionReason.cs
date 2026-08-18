namespace Forma.UI.Blazor.Components.Explorer;

/// <summary>Why an explorer refused to accept a resource.</summary>
public enum ExplorerRejectionReason
{
    /// <summary>The resource kind is not accepted by the explorer.</summary>
    ResourceKind,

    /// <summary>Adding the folder would exceed the maximum nesting depth.</summary>
    FolderDepth,

    /// <summary>The explorer already contains the maximum number of root folders.</summary>
    FolderRootCount,

    /// <summary>The folder already contains the maximum number of resources.</summary>
    FolderResourceCount,

    /// <summary>The parent already contains the maximum number of subfolders.</summary>
    SubfolderCount,

    /// <summary>The explorer already contains the maximum number of resources.</summary>
    ResourceCount,

    /// <summary>The explorer has reached its maximum total size.</summary>
    TotalBytes,
}
