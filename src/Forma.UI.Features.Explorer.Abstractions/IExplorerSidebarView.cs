namespace Forma.UI.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>What an explorer's folder tree shows, as the nodes drawing it read it.</summary>
public interface IExplorerSidebarView
{
    /// <summary>Which folder operations the sidebar offers.</summary>
    ExplorerFileOptions FileOptions { get; }

    /// <summary>What the explorer has selected.</summary>
    IExplorerSelectionView Selection { get; }

    /// <summary>The folders directly inside the parent, or the top-level folders when none is named.</summary>
    IReadOnlyList<FolderResourceInfo> ChildrenOf(string? parent);

    /// <summary>Whether the folder shows what it holds.</summary>
    bool IsExpanded(string folderSource);

    /// <summary>Whether the folder is the one being browsed.</summary>
    bool IsCurrent(FolderResourceInfo folder);
}
