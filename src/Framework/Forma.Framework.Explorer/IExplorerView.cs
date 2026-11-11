namespace Forma.Framework.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Navigation;
using Forma.Framework.Explorer.Selection;

/// <summary>Everything exposed by an explorer to the components drawing it.</summary>
public interface IExplorerView :
    IExplorerConfigurationView,
    IExplorerLocationView,
    IExplorerContentView,
    IExplorerSearchView
{
    /// <summary>What the explorer has selected.</summary>
    ICatalogSelectionView Selection { get; }

    /// <summary>Whether the item can be selected.</summary>
    /// <param name="item">The item.</param>
    bool CanSelect(ICatalogItem item);

    /// <summary>The folders from the top level down to the one a selected item stands in, where an empty list is the top level and null is a location the explorer doesn't know.</summary>
    /// <param name="item">The selected item.</param>
    /// <remarks>A folder stands in its parent, and a file in the location it was selected in, since a file names no folder of its own.</remarks>
    IReadOnlyList<ICatalogFolderItem>? GetSelectedItemLocation(ICatalogItem item);

    /// <summary>What the explorer exposes about its items.</summary>
    IExplorerItemView Item { get; }
}
