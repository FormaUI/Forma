namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;

/// <summary>What components presenting an explorer's current content may ask it to do.</summary>
public interface IExplorerContentInteraction
{
    /// <summary>Sorts by the field, reversing the direction when it is already the sorted field.</summary>
    Task SortByAsync(CatalogSortField field);

    /// <summary>Sets the sort direction.</summary>
    Task SetSortDirectionAsync(CatalogSortDirection direction);

    /// <summary>Gathers the items into groups by the field.</summary>
    Task GroupByAsync(ExplorerGroupField field);

    /// <summary>Changes the layout the content is shown in.</summary>
    Task ChangeViewModeAsync(ExplorerViewMode mode);

    /// <summary>Toggles whether the details view shows the column.</summary>
    Task ToggleDetailsColumnAsync(ExplorerDetailsColumns column);

    /// <summary>Shows or hides the details view's column header.</summary>
    Task SetDetailsHeaderVisibleAsync(bool isVisible);

    /// <summary>Refreshes the explorer's content.</summary>
    Task RefreshAsync();
}
