namespace Forma.Framework.Catalog.Selection;


/// <summary>What a surface has selected, as its parts read it.</summary>
public interface ICatalogSelectionView
{
    /// <summary>Whether the surface shows selection controls instead of activating previews.</summary>
    bool IsSelectionModeActive { get; }

    /// <summary>How many items are selected together.</summary>
    int Count { get; }

    /// <summary>The selected items.</summary>
    IReadOnlyList<ICatalogItem> Items { get; }

    /// <summary>Whether the item is selected.</summary>
    bool IsSelected(ICatalogItem item);
}
