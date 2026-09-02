namespace Forma.Features.Explorer;

/// <summary>What components presenting an explorer's items may ask it to do.</summary>
public interface IExplorerItemInteraction
{
    /// <summary>Toggles the item's selection in selection mode, and previews it otherwise.</summary>
    Task ActivateAsync(IExplorerItem item);

    /// <summary>Records the item as the one being dragged.</summary>
    void BeginDrag(IExplorerItem item);

    /// <summary>Drops the item being dragged onto the target, or the top level when null.</summary>
    Task DropOnItemAsync(IExplorerItem? target);

    /// <summary>Forgets the item being dragged.</summary>
    void EndDrag();

    /// <summary>Opens the new-folder dialog under the current folder.</summary>
    Task RequestNewFolderAsync();

    /// <summary>Opens the rename dialog for the item, with its current name as the draft.</summary>
    void RequestRename(IExplorerItem item);

    /// <summary>Opens confirmation for deleting the currently selected items.</summary>
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
