namespace Forma.Blazor.Components.DataView;

/// <summary>What a view asks of the data view it presents, on the reader's behalf.</summary>
/// <typeparam name="TItem">The item the data view manages.</typeparam>
public interface IDataViewInteraction<TItem>
    where TItem : notnull
{
    /// <summary>Registers a view: its key, what the view switch shows it as, and the viewport it keeps.</summary>
    /// <param name="registration">The view's registration.</param>
    void RegisterView(DataViewRegistration registration);

    /// <summary>Shows the view registered under <paramref name="key"/>.</summary>
    /// <param name="key">The view's key.</param>
    Task SwitchViewAsync(string key);

    /// <summary>Removes a view registered under <paramref name="key"/>.</summary>
    /// <param name="key">The view's key.</param>
    void UnregisterView(string key);

    /// <summary>Starts a pass over the items a view is about to draw.</summary>
    void BeginItemPass();

    /// <summary>Sorts by <paramref name="sort"/> under <paramref name="name"/>, in <paramref name="direction"/>.</summary>
    /// <param name="name">The name the sort is known and written to the address by.</param>
    /// <param name="sort">The sort to apply.</param>
    /// <param name="direction">Whether the sort applies as written or reversed.</param>
    Task SortByAsync(string name, DataViewSort<TItem> sort, DataViewSortDirection direction);

    /// <summary>Returns the items to the source's own order.</summary>
    Task ClearSortAsync();

    /// <summary>Folds <paramref name="group"/> away under its heading, or opens it again.</summary>
    /// <param name="group">The group whose heading was pressed.</param>
    Task ToggleGroupAsync(DataViewGroup<TItem> group);

    /// <summary>Asks the source for the roots once.</summary>
    Task LoadRootsAsync();

    /// <summary>Asks the source for the children of <paramref name="parent"/> once.</summary>
    /// <param name="parent">The node.</param>
    Task LoadChildrenAsync(TItem parent);

    /// <summary>Opens <paramref name="node"/>, asking the source for its children the first time.</summary>
    /// <param name="node">The node.</param>
    Task OpenNodeAsync(TItem node);

    /// <summary>Closes <paramref name="node"/>, hiding its children under it.</summary>
    /// <param name="node">The node.</param>
    Task CloseNodeAsync(TItem node);

    /// <summary>Makes <paramref name="item"/> the node whose children the views without a tree show, or shows the roots for null.</summary>
    /// <param name="item">The node, or null for the roots.</param>
    Task MakeCurrentAsync(TItem? item);

    /// <summary>Selects <paramref name="item"/> alone, as a plain press on it does, subject to the selection bounds.</summary>
    /// <param name="item">The item.</param>
    Task SelectItemAsync(TItem item);

    /// <summary>Selects or clears <paramref name="item"/> through its own control, subject to the selection bounds.</summary>
    /// <param name="item">The item whose control changed.</param>
    /// <param name="selected">Whether the item is to be selected.</param>
    Task ToggleSelectionAsync(TItem item, bool selected);

    /// <summary>Selects or clears what the master control reaches in the selection options' mode.</summary>
    Task ToggleMasterAsync();

    /// <summary>Appends the next page where the data view appends, a page already being asked for answering every call.</summary>
    Task LoadMoreAsync();

    /// <summary>Selects every item on screen that can be selected, subject to the selection bounds.</summary>
    Task SelectPageAsync();

    /// <summary>Selects every item of the set that can be selected where the data view holds it, else every such item on screen, subject to the selection bounds.</summary>
    Task SelectAllAsync();

    /// <summary>Clears the selection.</summary>
    Task ClearSelectionAsync();

    /// <summary>Reports that the reader activated <paramref name="item"/>.</summary>
    /// <param name="item">The item activated.</param>
    Task ActivateAsync(TItem item);
}
