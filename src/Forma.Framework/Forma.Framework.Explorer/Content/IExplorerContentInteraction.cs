namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items.Queries;

/// <summary>What components presenting an explorer's current content may ask it to do.</summary>
public interface IExplorerContentInteraction
{
    /// <summary>Sorts by the field, reversing the direction when it is already the sorted field.</summary>
    Task SortByAsync(ExplorerSortField field);

    /// <summary>Sets the sort direction outright.</summary>
    Task SetSortDirectionAsync(ExplorerSortDirection direction);

    /// <summary>Gathers the items into groups by the field, or by none.</summary>
    Task GroupByAsync(ExplorerGroupField field);

    /// <summary>Folds the group's items away when they are shown and shows them when they are folded away.</summary>
    Task ToggleGroupAsync(ExplorerItemGroup group);

    /// <summary>Switches which layout the content is shown in.</summary>
    Task ChangeViewModeAsync(ExplorerViewMode mode);

    /// <summary>Shows the details column when it is hidden and hides it when it is shown.</summary>
    Task ToggleDetailsColumnAsync(ExplorerDetailsColumns column);

    /// <summary>Shows or hides the details view's column header.</summary>
    Task SetDetailsHeaderVisibleAsync(bool isVisible);

    /// <summary>Fetches the next page, appending it to what is already shown.</summary>
    Task RequestNextPageAsync();

    /// <summary>Replaces what is shown with the given page.</summary>
    Task ChangePageAsync(int page);

    /// <summary>Refreshes the explorer's content.</summary>
    Task RefreshAsync();
}
