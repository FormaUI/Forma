namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>What the components drawing an explorer may ask it to do.</summary>
public interface IExplorerInteraction
{
    /// <summary>Toggles the resource's selection in selection mode, and previews it otherwise.</summary>
    Task ActivateAsync(ResourceInfo resource);

    /// <summary>Adds the resource to the selection, or removes it when it is already selected.</summary>
    Task ToggleSelectAsync(ResourceInfo resource);

    /// <summary>Adds the folder to the selection, or removes it when it is already selected.</summary>
    Task ToggleFolderSelectAsync(FolderResourceInfo folder);

    /// <summary>Sorts by the field, reversing the direction when it is already the sorted field.</summary>
    Task SortByAsync(ExplorerSortField field);

    /// <summary>Records the resource as the one being dragged.</summary>
    void BeginDrag(ResourceInfo resource);

    /// <summary>Switches which layout the media is shown in.</summary>
    Task ChangeViewModeAsync(ExplorerViewMode mode);

    /// <summary>Fetches the next page, appending it to what is already shown.</summary>
    Task RequestNextPageAsync();

    /// <summary>Replaces what is shown with the given page.</summary>
    Task ChangePageAsync(int page);
}
