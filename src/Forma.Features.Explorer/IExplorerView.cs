namespace Forma.Features.Explorer;


/// <summary>What an explorer shows, as the components drawing it read it.</summary>
public interface IExplorerView
{
    /// <summary>How the explorer presents itself.</summary>
    ExplorerOptions Options { get; }

    /// <summary>The folder list indexed, so the tree and the breadcrumb do not each rescan it.</summary>
    ExplorerFolderTree FolderTree { get; }

    /// <summary>The field the explorer is sorted by.</summary>
    ExplorerSortField SortField { get; }

    /// <summary>The direction that order runs in.</summary>
    ExplorerSortDirection SortDirection { get; }

    /// <summary>What the explorer has selected.</summary>
    IExplorerSelectionView Selection { get; }

    /// <summary>The operations offered on every media resource, in render order.</summary>
    IReadOnlyList<ExplorerAction> Actions { get; }
}
