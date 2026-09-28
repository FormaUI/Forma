namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Events;

using Microsoft.AspNetCore.Components;

/// <summary>What a view reads of the data view it presents: the items on screen and the state around them, announced through <see cref="INotifyStateChanged"/> as it changes.</summary>
/// <typeparam name="TItem">The item the data view browses.</typeparam>
public interface IDataView<TItem> : INotifyStateChanged
    where TItem : notnull
{
    /// <summary>The items on screen, in the order shown.</summary>
    IReadOnlyList<TItem> Items { get; }

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

    /// <summary>Whether a view renders only the items near the scroll viewport, fetching windows through <see cref="LoadWindowAsync"/>.</summary>
    bool Virtualize { get; }

    /// <summary>The height of the unit a view virtualizes by, in pixels.</summary>
    float ItemHeight { get; }

    /// <summary>How many units a view renders beyond each edge of the viewport while virtualizing.</summary>
    int OverscanCount { get; }

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

    /// <summary>How many items are selected, on screen or not.</summary>
    int SelectedCount { get; }

    /// <summary>Whether the data view holds the whole set, so it can select all of it and say how many that is.</summary>
    bool HoldsSet { get; }

    /// <summary>Whether selecting the whole set is worth offering: the set is held and holds more than is on screen.</summary>
    bool CanOfferSet { get; }

    /// <summary>Identifies an item across reloads.</summary>
    Func<TItem, object> ItemKey { get; }

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

    /// <summary>The items of one window, sorted as the data view is, for a view that virtualizes.</summary>
    /// <param name="startIndex">The index of the first item asked for.</param>
    /// <param name="count">How many items are asked for.</param>
    /// <param name="cancellationToken">Canceled when the window is no longer wanted.</param>
    ValueTask<DataViewPage<TItem>> LoadWindowAsync(int startIndex, int count, CancellationToken cancellationToken);
}
