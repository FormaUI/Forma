namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>What components presenting an explorer's current content may ask it to do.</summary>
public interface IExplorerContentInteraction
{
    /// <summary>Toggles the resource's selection in selection mode, and previews it otherwise.</summary>
    Task ActivateAsync(ResourceInfo resource);

    /// <summary>Sorts by the field, reversing the direction when it is already the sorted field.</summary>
    Task SortByAsync(ExplorerSortField field);

    /// <summary>Sets the sort direction outright.</summary>
    Task SetSortDirectionAsync(ExplorerSortDirection direction);

    /// <summary>Records the resource as the one being dragged.</summary>
    void BeginDrag(ResourceInfo resource);

    /// <summary>Drops whatever is being dragged onto the folder, or the top level when null.</summary>
    Task DropOnFolderAsync(FolderResourceInfo? folder);

    /// <summary>Switches which layout the content is shown in.</summary>
    Task ChangeViewModeAsync(ExplorerViewMode mode);

    /// <summary>Fetches the next page, appending it to what is already shown.</summary>
    Task RequestNextPageAsync();

    /// <summary>Replaces what is shown with the given page.</summary>
    Task ChangePageAsync(int page);

    /// <summary>Opens the new-folder dialog under the current folder.</summary>
    Task RequestNewFolderAsync();

    /// <summary>Shows or hides the upload panel.</summary>
    void ToggleUpload();

    /// <summary>Refreshes the explorer's content.</summary>
    Task RefreshAsync();
}
