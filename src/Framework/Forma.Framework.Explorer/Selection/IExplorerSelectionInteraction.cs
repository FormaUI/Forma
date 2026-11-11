namespace Forma.Framework.Explorer.Selection;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Explorer.Items;

/// <summary>What components managing an explorer's selection may ask it to do.</summary>
public interface IExplorerSelectionInteraction
{
    /// <summary>Toggles whether the item is selected.</summary>
    Task ToggleSelectionAsync(ICatalogItem item);

    /// <summary>Enters or leaves selection mode.</summary>
    Task ToggleSelectionModeAsync();

    /// <summary>Clears the current selection.</summary>
    Task ClearSelectionAsync();

    /// <summary>Takes the item out of the selection.</summary>
    Task DeselectAsync(ICatalogItem item);
}
