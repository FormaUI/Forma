namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Catalog;

/// <summary>What components presenting an explorer's items may ask it to do.</summary>
public interface IExplorerItemInteraction
{
    /// <summary>Starts dragging the item.</summary>
    void BeginDrag(ICatalogItem item);

    /// <summary>Drops the dragged item onto the target, or onto the top level when null.</summary>
    Task DropOnItemAsync(ICatalogItem? target);

    /// <summary>Ends the drag.</summary>
    void EndDrag();

    /// <summary>Opens the new-folder dialog under the current folder.</summary>
    Task RequestNewFolderAsync();

    /// <summary>Opens the rename dialog for the item.</summary>
    void RequestRename(ICatalogItem item);

    /// <summary>Opens confirmation for deleting the selected items.</summary>
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
