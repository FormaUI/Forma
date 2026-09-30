namespace Forma.Framework.Explorer.Navigation;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Selection;

/// <summary>What an explorer's folder tree shows, as the nodes drawing it read it.</summary>
public interface IExplorerSidebarView
{
    /// <summary>Which folder operations the sidebar offers.</summary>
    ExplorerItemOptions ItemOptions { get; }

    /// <summary>What the explorer has selected.</summary>
    ICatalogSelectionView Selection { get; }

    /// <summary>The listed folders directly inside the parent, or the top-level ones when null.</summary>
    IReadOnlyList<ICatalogFolderItem> GetChildren(string? parent);

    /// <summary>Whether the folder shows what it holds.</summary>
    bool IsExpanded(string folderKey);

    /// <summary>Whether the folder is the one being browsed.</summary>
    bool IsCurrent(ICatalogFolderItem item);
}
