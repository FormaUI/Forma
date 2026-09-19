namespace Forma.Framework.Explorer;

using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Items.Navigation;
using Forma.Framework.Explorer.Items.Queries;
using Forma.Framework.Explorer.Items.Selection;
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
    IExplorerSelectionView Selection { get; }

    /// <summary>The explorer's items.</summary>
    IExplorerItemView Item { get; }
}
