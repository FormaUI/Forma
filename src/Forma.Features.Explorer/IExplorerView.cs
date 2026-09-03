namespace Forma.Features.Explorer;

using Forma.Features.Explorer.Content;
using Forma.Features.Explorer.Content.Queries;
using Forma.Features.Explorer.Items;
using Forma.Features.Explorer.Navigation;
using Forma.Features.Explorer.Selection;

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
