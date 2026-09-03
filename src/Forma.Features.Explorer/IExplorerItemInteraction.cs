namespace Forma.Features.Explorer;

/// <summary>What components presenting an explorer's items may ask it to do.</summary>
public interface IExplorerItemInteraction
{
    /// <summary>Toggles the selection of an enabled item in selection mode, and previews it otherwise.</summary>
    Task ActivateAsync(IExplorerItem item);

    /// <summary>Records a draggable item as the one being dragged.</summary>
    void BeginDrag(IExplorerItem item);

    /// <summary>Drops the movable item being dragged onto a folder that accepts drops, or the top level when null.</summary>
    Task DropOnItemAsync(IExplorerItem? target);

    /// <summary>Forgets the item being dragged.</summary>
    void EndDrag();

    /// <summary>Opens the new-folder dialog under the current folder.</summary>
    Task RequestNewFolderAsync();

    /// <summary>Opens the rename dialog for an item that can be renamed, with its current name as the draft.</summary>
    void RequestRename(IExplorerItem item);

    /// <summary>Opens confirmation for deleting those of the selected items that can be deleted.</summary>
    void RequestDeleteSelected();

    /// <summary>Sets the draft name of the pending named action.</summary>
    void SetActionName(string name);

    /// <summary>Cancels the action currently awaiting confirmation.</summary>
    void CancelAction();

    /// <summary>Confirms the action currently awaiting confirmation.</summary>
    Task ConfirmActionAsync();

    /// <summary>Shows or hides the upload panel.</summary>
    void ToggleUpload();
}
