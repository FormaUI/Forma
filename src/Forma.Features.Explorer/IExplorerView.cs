namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>What an explorer shows, as the components drawing it read it.</summary>
public interface IExplorerView
{
    /// <summary>How the explorer presents itself.</summary>
    ExplorerOptions Options { get; }

    /// <summary>The folder list indexed, so the tree and the breadcrumb do not each rescan it.</summary>
    ExplorerFolderTree FolderTree { get; }

    /// <summary>The folder currently open, or null at the top level.</summary>
    FolderResourceInfo? CurrentFolder { get; }

    /// <summary>The field the explorer is sorted by.</summary>
    ExplorerSortField SortField { get; }

    /// <summary>The direction that order runs in.</summary>
    ExplorerSortDirection SortDirection { get; }

    /// <summary>Which layout the media is shown in.</summary>
    ExplorerViewMode ViewMode { get; }

    /// <summary>What the explorer has selected.</summary>
    IExplorerSelectionView Selection { get; }

    /// <summary>The operations offered on every media resource, in render order.</summary>
    IReadOnlyList<ExplorerAction> Actions { get; }

    /// <summary>The resources on screen.</summary>
    IReadOnlyList<ResourceInfo> VisibleResources { get; }

    /// <summary>Whether a provider fetch is in flight.</summary>
    bool IsFetching { get; }

    /// <summary>How many resources match the current search and sort, or null when that cannot be said.</summary>
    int? MatchingCount { get; }

    /// <summary>How the currently shown resources are paged.</summary>
    ExplorerPagingInfo Paging { get; }
}
