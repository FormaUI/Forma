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

    /// <summary>The explorer's items.</summary>
    IExplorerItemView Item { get; }
}
