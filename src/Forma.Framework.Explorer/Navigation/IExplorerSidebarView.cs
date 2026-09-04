namespace Forma.Framework.Explorer.Navigation;

using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Selection;

/// <summary>What an explorer's folder tree shows, as the nodes drawing it read it.</summary>
public interface IExplorerSidebarView
{
    /// <summary>Which folder operations the sidebar offers.</summary>
    ExplorerItemOptions ItemOptions { get; }

    /// <summary>What the explorer has selected.</summary>
    IExplorerSelectionView Selection { get; }

    /// <summary>The folders directly inside the parent that are not hidden, or the top-level ones when none is named.</summary>
    IReadOnlyList<IExplorerFolderItem> ChildrenOf(string? parent);

    /// <summary>Whether the folder shows what it holds.</summary>
    bool IsExpanded(string folderKey);

    /// <summary>Whether the folder is the one being browsed.</summary>
    bool IsCurrent(IExplorerFolderItem item);
}
