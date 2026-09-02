namespace Forma.Features.Explorer;

/// <summary>An explorer's location in its folders, as the components drawing it read it.</summary>
public interface IExplorerLocationView
{
    /// <summary>The folder currently open, or null at the top level.</summary>
    IExplorerFolderItem? CurrentFolder { get; }

    /// <summary>The path from the root to the current folder.</summary>
    IReadOnlyList<IExplorerFolderItem> Breadcrumbs { get; }

    /// <summary>The indexed folder hierarchy used by the tree and breadcrumb.</summary>
    ExplorerFolderTree FolderTree { get; }

    /// <summary>Whether a previous folder is available to return to.</summary>
    bool CanGoBack { get; }

    /// <summary>Whether a folder left by going back is available to return to.</summary>
    bool CanGoForward { get; }

    /// <summary>Whether the current folder has a parent to go up to.</summary>
    bool CanGoUp { get; }
}
