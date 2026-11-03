namespace Forma.Framework.Catalog.Selection;


/// <summary>The read-only view of a surface's selection.</summary>
public interface ICatalogSelectionView
{
    /// <summary>Whether selection mode is active, with the surface showing selection controls.</summary>
    bool IsSelectionModeActive { get; }

    /// <summary>The number of selected items.</summary>
    int Count { get; }

    /// <summary>The selected items.</summary>
    IReadOnlyList<ICatalogItem> Items { get; }

    /// <summary>Whether the item is selected.</summary>
    bool IsSelected(ICatalogItem item);
}
