namespace Forma.UI.Components.Gallery;

/// <summary>Why a gallery refused to accept a resource.</summary>
public enum GalleryRejectionReason
{
    /// <summary>The resource kind is not accepted by the gallery.</summary>
    ResourceKind,

    /// <summary>Adding the folder would exceed the maximum nesting depth.</summary>
    FolderDepth,

    /// <summary>The gallery already contains the maximum number of root folders.</summary>
    FolderRootCount,

    /// <summary>The folder already contains the maximum number of resources.</summary>
    FolderResourceCount,

    /// <summary>The parent already contains the maximum number of subfolders.</summary>
    SubfolderCount,

    /// <summary>The gallery already contains the maximum number of resources.</summary>
    ResourceCount,

    /// <summary>The gallery has reached its maximum total size.</summary>
    TotalBytes,
}
