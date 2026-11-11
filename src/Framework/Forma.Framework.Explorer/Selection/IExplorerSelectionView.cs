namespace Forma.Framework.Explorer.Selection;

using Forma.Framework.Catalog;

/// <summary>What an explorer has selected, which the components drawing it read.</summary>
public interface IExplorerSelectionView
{
    /// <summary>Whether selection mode is active, with the explorer drawing a box on every item.</summary>
    bool IsSelectionModeActive { get; }

    /// <summary>The number of selected items.</summary>
    int Count { get; }

    /// <summary>The selected items, folders first.</summary>
    IReadOnlyList<ICatalogItem> Items { get; }

    /// <summary>Whether the item is selected.</summary>
    /// <param name="item">The item.</param>
    bool IsSelected(ICatalogItem item);
}
