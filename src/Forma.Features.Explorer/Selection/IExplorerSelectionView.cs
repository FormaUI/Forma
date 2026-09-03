namespace Forma.Features.Explorer.Selection;

using Forma.Features.Explorer.Items;

/// <summary>What an explorer has selected, as components read it.</summary>
public interface IExplorerSelectionView
{
    /// <summary>Whether the explorer shows selection controls instead of activating previews.</summary>
    bool IsSelectionModeActive { get; }

    /// <summary>How many items are selected together.</summary>
    int Count { get; }

    /// <summary>The selected items.</summary>
    IReadOnlyList<IExplorerItem> Items { get; }

    /// <summary>Whether the item is selected.</summary>
    bool IsSelected(IExplorerItem item);
}
