namespace Forma.UI.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>What the nodes drawing an explorer's folder tree may ask it to do.</summary>
public interface IExplorerSidebarInteraction
{
    /// <summary>Shows what the folder holds, or hides it when it is already shown.</summary>
    void ToggleExpanded(string folderSource);

    /// <summary>Browses the folder.</summary>
    Task SelectFolderAsync(FolderResourceInfo? folder);

    /// <summary>Asks for a new folder under the parent, or at the top level when none is named.</summary>
    Task CreateFolderAsync(FolderResourceInfo? parent);

    /// <summary>Asks to rename the folder.</summary>
    Task RenameFolderAsync(FolderResourceInfo folder);

    /// <summary>Asks to delete the folder.</summary>
    Task DeleteFolderAsync(FolderResourceInfo folder);

    /// <summary>Reports what was dropped onto the folder.</summary>
    Task DropOnFolderAsync(FolderResourceInfo? folder);

    /// <summary>Records the folder as the one being dragged.</summary>
    Task BeginFolderDragAsync(FolderResourceInfo folder);

    /// <summary>Adds the folder to the selection, or removes it when it is already selected.</summary>
    Task ToggleFolderSelectAsync(FolderResourceInfo folder);
}
