namespace Forma.Blazor.Components.DataView;

/// <summary>What a view asks of the data view it presents, on the reader's behalf.</summary>
/// <typeparam name="TItem">The item the data view browses.</typeparam>
public interface IDataViewInteraction<TItem>
    where TItem : notnull
{
    /// <summary>Registers a view under the key the view switch and the address name it by, with the viewport it keeps.</summary>
    /// <param name="key">The view's key.</param>
    /// <param name="viewport">The view's viewport.</param>
    void RegisterView(string key, IDataViewViewport viewport);

    /// <summary>Removes a view registered under <paramref name="key"/>.</summary>
    /// <param name="key">The view's key.</param>
    void UnregisterView(string key);

    /// <summary>Starts a pass over the items a view is about to draw, so the tokens of items that left can be released.</summary>
    void BeginItemPass();

    /// <summary>Sorts by <paramref name="sort"/> under <paramref name="name"/>, in <paramref name="direction"/>.</summary>
    /// <param name="name">The name the sort is known and written to the address by.</param>
    /// <param name="sort">The sort to apply.</param>
    /// <param name="direction">Whether the sort applies as written or reversed.</param>
    Task SortByAsync(string name, DataViewSort<TItem> sort, DataViewSortDirection direction);

    /// <summary>Returns the items to the source's own order.</summary>
    Task ClearSortAsync();

    /// <summary>Selects or clears <paramref name="item"/> through its own control, which the selection bounds may refuse.</summary>
    /// <param name="item">The item whose control changed.</param>
    /// <param name="selected">Whether the item is to be selected.</param>
    Task ToggleSelectionAsync(TItem item, bool selected);

    /// <summary>Selects or clears what the master control reaches in the selection options' mode.</summary>
    Task ToggleMasterAsync();

    /// <summary>Selects every item on screen, which the selection bounds may refuse.</summary>
    Task SelectPageAsync();

    /// <summary>Selects every item of the set where the data view holds it, else every item on screen; the selection bounds may refuse.</summary>
    Task SelectAllAsync();

    /// <summary>Clears the selection, which is always allowed.</summary>
    Task ClearSelectionAsync();

    /// <summary>Reports that the reader activated <paramref name="item"/> outside any selection gesture.</summary>
    /// <param name="item">The item activated.</param>
    Task ActivateAsync(TItem item);
}
