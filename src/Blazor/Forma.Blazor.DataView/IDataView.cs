namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Events;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>What a view reads of the data view it presents: the items on screen and the state around them, announced through <see cref="INotifyStateChanged"/> as it changes.</summary>
/// <typeparam name="TItem">The item the data view manages.</typeparam>
public interface IDataView<TItem> : INotifyStateChanged
    where TItem : notnull
{
    /// <summary>The key of the view showing.</summary>
    string? ActiveViewKey { get; }

    /// <summary>The name an item goes by.</summary>
    Func<TItem, string> ItemName { get; }

    /// <summary>The items on screen, in the order shown.</summary>
    IReadOnlyList<TItem> Items { get; }

    /// <summary>The items on screen under the headings they fold under, in the order shown.</summary>
    IReadOnlyList<DataViewGroup<TItem>> Groups { get; }

    /// <summary>Whether the items on screen stand under group headings.</summary>
    bool IsGrouped { get; }

    /// <summary>Whether <paramref name="group"/> is folded, its items hidden under its heading.</summary>
    /// <param name="group">The group.</param>
    bool IsFolded(DataViewGroup<TItem> group);

    /// <summary>Whether the source has parentage, its items standing under nodes.</summary>
    bool HasHierarchy { get; }

    /// <summary>The node whose children the views without a tree show, where null means the roots.</summary>
    TItem? CurrentItem { get; }

    /// <summary>The roots the source has answered with.</summary>
    IReadOnlyList<TItem> Roots { get; }

    /// <summary>Whether the roots have been asked for and answered.</summary>
    bool AreRootsLoaded { get; }

    /// <summary>Whether the roots are being asked for.</summary>
    bool IsLoadingRoots { get; }

    /// <summary>The children of <paramref name="parent"/> the source has answered with.</summary>
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

    /// <summary>How many times the view has changed in a way that can change how any item is drawn, a change of one item's own state apart: its selection, its being open or loading its children, a group's folding.</summary>
    int Revision { get; }

    /// <summary>Whether the source has answered at least once.</summary>
    bool HasLoaded { get; }

    /// <summary>Whether the source is being asked and has not answered yet.</summary>
    bool IsBusy { get; }

    /// <summary>How many items the whole set holds, where null means the source has not said.</summary>
    int? TotalItemCount { get; }

    /// <summary>The name of the active sort, where null means the items stand in the source's own order.</summary>
    string? SortName { get; }

    /// <summary>Whether a view renders only the items near the scroll viewport, which it reads a slice at a time.</summary>
    bool IsVirtualizing { get; }

    /// <summary>How many items a view lays out: the whole set's where the data view counts it, the items loaded so far while it appends.</summary>
    int? ItemCount { get; }

    /// <summary>What a view draws after its items while the data view appends: the action loading the next page, which the reader reaching the end also starts.</summary>
    RenderFragment? MoreContent { get; }

    /// <summary>Who arranges the views and the footer: the data view, or the host inside it.</summary>
    DataViewLayout Layout { get; }

    /// <summary>Whether a sort always applies, so a header turns a sort's direction rather than clearing it.</summary>
    bool RequiresSort { get; }

    /// <summary>Whether the active sort applies as written or reversed.</summary>
    DataViewSortDirection SortDirection { get; }

    /// <summary>The name of the sort the address asked for and no view has applied yet.</summary>
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

    /// <summary>Whether the data view holds the whole set.</summary>
    bool HoldsSet { get; }

    /// <summary>Whether selecting the whole set is worth offering: the set is held and holds more than is on screen.</summary>
    bool CanOfferSet { get; }

    /// <summary>Reads the key an item is identified by.</summary>
    Func<TItem, object> ItemKey { get; }

    /// <summary>Compares items by <see cref="ItemKey"/>.</summary>
    IEqualityComparer<TItem> ItemComparer { get; }

    /// <summary>The class the caller gives an item.</summary>
    Func<TItem, string?>? ItemClass { get; }

    /// <summary>The attributes and event handlers the caller gives the element a view draws for an item.</summary>
    Func<TItem, IReadOnlyDictionary<string, object>?>? ItemAttributes { get; }

    /// <summary>What a view shows in place of its items while the source has not answered yet.</summary>
    RenderFragment LoadingContent { get; }

    /// <summary>What a view shows in place of its items when the source answered with none.</summary>
    RenderFragment EmptyContent { get; }

    /// <summary>Whether <paramref name="item"/> is selected.</summary>
    /// <param name="item">The item to judge.</param>
    bool IsSelected(TItem item);

    /// <summary>Whether <paramref name="item"/>'s own selection control can act within the selection bounds.</summary>
    /// <param name="item">The item to judge.</param>
    bool CanInteractWith(TItem item);

    /// <summary>Whether <paramref name="item"/> is disabled, neither selected nor activated.</summary>
    /// <param name="item">The item to judge.</param>
    bool IsDisabled(TItem item);

    /// <summary>Whether <paramref name="item"/> can be selected: it is not disabled, and the host lets it be.</summary>
    /// <param name="item">The item to judge.</param>
    bool IsSelectable(TItem item);

    /// <summary>Whether <paramref name="item"/> can be activated: it is not disabled, and the host lets it be.</summary>
    /// <param name="item">The item to judge.</param>
    bool CanActivate(TItem item);

    /// <summary>Whether <paramref name="item"/> can take the focus, which a view draws a marker for.</summary>
    /// <param name="item">The item to judge.</param>
    bool CanFocus(TItem item);

    /// <summary>Whether <paramref name="item"/> is drawn muted: it is disabled, or it can be selected and the selection's maximum blocks it.</summary>
    /// <param name="item">The item to judge.</param>
    bool IsMuted(TItem item);

    /// <summary>The token the selection behavior knows <paramref name="item"/> by while it stays on screen.</summary>
    /// <param name="item">The item on screen.</param>
    int GetItemToken(TItem item);

    /// <summary>The token the selection behavior knows <paramref name="item"/> by, drawn by <paramref name="view"/> in its item passes.</summary>
    /// <param name="item">The item on screen.</param>
    /// <param name="view">The view drawing it.</param>
    int GetItemToken(TItem item, IDataViewViewport view);

    /// <summary>Reads one slice of the items.</summary>
    /// <param name="startIndex">The index of the first item asked for.</param>
    /// <param name="count">How many items are asked for.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    ValueTask<DataViewPage<TItem>> LoadSliceAsync(int startIndex, int count, CancellationToken cancellationToken);
}
