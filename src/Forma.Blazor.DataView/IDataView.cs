namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Events;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>What a view reads of the data view it presents: the items on screen and the state around them, announced through <see cref="INotifyStateChanged"/> as it changes.</summary>
/// <typeparam name="TItem">The item the data view manages.</typeparam>
public interface IDataView<TItem> : INotifyStateChanged
    where TItem : notnull
{
    /// <summary>The key of the view showing, which a view compares with its own to know whether to draw; null while none has registered.</summary>
    string? ActiveViewKey { get; }

    /// <summary>The name an item goes by, which a view without columns shows for it.</summary>
    Func<TItem, string> ItemName { get; }

    /// <summary>The items on screen, in the order shown.</summary>
    IReadOnlyList<TItem> Items { get; }

    /// <summary>The items on screen under the headings they fold under, in the order shown; empty when nothing groups them.</summary>
    IReadOnlyList<DataViewGroup<TItem>> Groups { get; }

    /// <summary>Whether the items on screen stand under group headings.</summary>
    bool IsGrouped { get; }

    /// <summary>Whether <paramref name="group"/> is folded, its items hidden under its heading.</summary>
    /// <param name="group">The group.</param>
    bool IsFolded(DataViewGroup<TItem> group);

    /// <summary>Whether the source has parentage, so items stand under nodes and the tree has something to show.</summary>
    bool HasHierarchy { get; }

    /// <summary>The node whose children the views without a tree show, or null for the roots.</summary>
    TItem? CurrentItem { get; }

    /// <summary>The roots the source has answered with; empty until they have loaded.</summary>
    IReadOnlyList<TItem> Roots { get; }

    /// <summary>Whether the roots have been asked for and answered.</summary>
    bool AreRootsLoaded { get; }

    /// <summary>Whether the roots are being asked for.</summary>
    bool IsLoadingRoots { get; }

    /// <summary>The children of <paramref name="parent"/> the source has answered with; empty until they have loaded.</summary>
    /// <param name="parent">The node.</param>
    IReadOnlyList<TItem> GetChildren(TItem parent);

    /// <summary>Whether the children of <paramref name="parent"/> have been asked for and answered.</summary>
    /// <param name="parent">The node.</param>
    bool AreChildrenLoaded(TItem parent);

    /// <summary>Whether the children of <paramref name="parent"/> are being asked for.</summary>
    /// <param name="parent">The node.</param>
    bool IsLoadingChildren(TItem parent);

    /// <summary>The parent of <paramref name="node"/> as the source answered it, null for a root; false for a node the source has not answered under any parent.</summary>
    /// <param name="node">The node.</param>
    /// <param name="parent">The node's parent, or null for a root.</param>
    bool TryGetParent(TItem node, out TItem? parent);

    /// <summary>Whether <paramref name="node"/> can open: the source says it has children, or has not yet said it has none.</summary>
    /// <param name="node">The node.</param>
    bool CanOpen(TItem node);

    /// <summary>Whether <paramref name="node"/> is open, its children shown under it.</summary>
    /// <param name="node">The node.</param>
    bool IsOpen(TItem node);

    /// <summary>Whether the source has answered at least once.</summary>
    bool HasLoaded { get; }

    /// <summary>Whether the source is being asked and has not answered yet.</summary>
    bool IsBusy { get; }

    /// <summary>How many items the whole set holds, or null while the source has not said.</summary>
    int? TotalItemCount { get; }

    /// <summary>The name of the active sort, or null when the items are in the source's own order.</summary>
    string? SortName { get; }

    /// <summary>Whether the active sort applies as written or reversed.</summary>
    DataViewSortDirection SortDirection { get; }

    /// <summary>The name of the sort the address asked for and no view has applied yet, or null when there is none.</summary>
    string? RequestedSortName { get; }

    /// <summary>The direction the address asked the requested sort to apply in.</summary>
    DataViewSortDirection RequestedSortDirection { get; }

    /// <summary>Whether the items are shown a page at a time.</summary>
    bool EnablePagination { get; }

    /// <summary>Whether a view renders only the items near the scroll viewport.</summary>
    bool Virtualize { get; }

    /// <summary>The height of the unit a view virtualizes by, in pixels.</summary>
    float ItemHeight { get; }

    /// <summary>How many units a view renders beyond each edge of the viewport while virtualizing.</summary>
    int OverscanCount { get; }

    /// <summary>Where the viewport holds when items arrive at an edge while virtualizing.</summary>
    VirtualizeAnchorMode AnchorMode { get; }

    /// <summary>The index of the item the data view opens on while virtualizing, applied once.</summary>
    int InitialItemIndex { get; }

    /// <summary>Whether every item has a box to check.</summary>
    bool EnableCheckedSelection { get; }

    /// <summary>Whether items are selected through pointer and keyboard gestures on themselves.</summary>
    bool EnableItemSelection { get; }

    /// <summary>The checked selection's bounds and the caption of its master control.</summary>
    CheckedSelectionOptions CheckedSelectionOptions { get; }

    /// <summary>Whether every item on screen is selected.</summary>
    bool AllSelected { get; }

    /// <summary>Whether some items on screen are selected and others are not.</summary>
    bool PartiallySelected { get; }

    /// <summary>Whether the master control can neither select nor clear anything.</summary>
    bool IsMasterControlDisabled { get; }

    /// <summary>Whether a master control is drawn: the selection options ask for one and there is something for it to reach.</summary>
    bool HasMasterControl { get; }

    /// <summary>Whether a menu stands beside the master control offering the page, the set and clearing.</summary>
    bool HasMasterMenu { get; }

    /// <summary>How many items are selected, on screen or not.</summary>
    int SelectedCount { get; }

    /// <summary>Whether the data view holds the whole set, so it can select all of it and say how many that is.</summary>
    bool HoldsSet { get; }

    /// <summary>Whether selecting the whole set is worth offering: the set is held and holds more than is on screen.</summary>
    bool CanOfferSet { get; }

    /// <summary>Identifies an item across reloads.</summary>
    Func<TItem, object> ItemKey { get; }

    /// <summary>Compares items by <see cref="ItemKey"/>, as the selection does.</summary>
    IEqualityComparer<TItem> ItemComparer { get; }

    /// <summary>The class the caller gives an item, or null for none.</summary>
    Func<TItem, string?>? ItemClass { get; }

    /// <summary>What a view shows in place of its items while the source has not answered yet.</summary>
    RenderFragment LoadingContent { get; }

    /// <summary>What a view shows in place of its items when the source answered with none.</summary>
    RenderFragment EmptyContent { get; }

    /// <summary>Whether <paramref name="item"/> is selected.</summary>
    /// <param name="item">The item to judge.</param>
    bool IsSelected(TItem item);

    /// <summary>Whether <paramref name="item"/>'s own selection control can still act, which the selection bounds may deny.</summary>
    /// <param name="item">The item to judge.</param>
    bool CanInteractWith(TItem item);

    /// <summary>The token the selection behavior knows <paramref name="item"/> by while it stays on screen.</summary>
    /// <param name="item">The item on screen.</param>
    int GetItemToken(TItem item);

    /// <summary>Reads one slice of the items.</summary>
    /// <param name="startIndex">The index of the first item asked for.</param>
    /// <param name="count">How many items are asked for.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    ValueTask<DataViewPage<TItem>> LoadSliceAsync(int startIndex, int count, CancellationToken cancellationToken);
}
