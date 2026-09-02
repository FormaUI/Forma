namespace Forma.Features.Explorer;

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
