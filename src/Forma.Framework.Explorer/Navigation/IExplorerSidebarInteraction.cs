namespace Forma.Framework.Explorer.Navigation;

using Forma.Framework.Explorer.Items;

/// <summary>What the nodes drawing an explorer's folder tree may ask it to do.</summary>
public interface IExplorerSidebarInteraction
{
    /// <summary>Shows what the folder holds, or hides it when it is already shown.</summary>
    void ToggleExpanded(string folderKey);

    /// <summary>Browses the folder, or the top level when none is named.</summary>
    Task SelectFolderAsync(IExplorerFolderItem? item);

    /// <summary>Asks for a new folder under the parent, or at the top level when none is named.</summary>
    Task CreateFolderAsync(IExplorerFolderItem? parent);

    /// <summary>Asks to rename the folder.</summary>
    Task RenameFolderAsync(IExplorerFolderItem item);

    /// <summary>Asks to delete the folder.</summary>
    Task DeleteFolderAsync(IExplorerFolderItem item);

    /// <summary>Reports what was dropped onto the folder.</summary>
    Task DropOnFolderAsync(IExplorerFolderItem? item);

    /// <summary>Records the folder as the one being dragged.</summary>
    Task BeginFolderDragAsync(IExplorerFolderItem item);

    /// <summary>Adds the folder to the selection, or removes it when it is already selected.</summary>
    Task ToggleFolderSelectAsync(IExplorerFolderItem item);
}
