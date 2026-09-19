namespace Forma.Framework.Explorer.Selection;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Explorer.Items;

/// <summary>What components managing an explorer's selection may ask it to do.</summary>
public interface IExplorerSelectionInteraction
{
    /// <summary>Adds the item to the selection, or removes it when already selected.</summary>
    Task ToggleSelectionAsync(ICatalogItem item);

    /// <summary>Enters or leaves selection mode, clearing the selection on the way out.</summary>
    Task ToggleSelectionModeAsync();

    /// <summary>Clears the current selection.</summary>
    Task ClearSelectionAsync();
}
