namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>What components managing an explorer's selection may ask it to do.</summary>
public interface IExplorerSelectionInteraction
{
    /// <summary>Adds the resource to the selection, or removes it when already selected.</summary>
    Task ToggleSelectionAsync(ResourceInfo resource);

    /// <summary>Adds the folder to the selection, or removes it when already selected.</summary>
    Task ToggleFolderSelectAsync(FolderResourceInfo folder);

    /// <summary>Enters or leaves selection mode, clearing the selection on the way out.</summary>
    Task ToggleSelectionModeAsync();

    /// <summary>Clears the current selection.</summary>
    Task ClearSelectionAsync();

    /// <summary>Opens confirmation for deleting the current selection.</summary>
    void RequestDeleteSelected();
}
