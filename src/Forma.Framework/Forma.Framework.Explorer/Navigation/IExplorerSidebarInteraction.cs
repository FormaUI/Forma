namespace Forma.Framework.Explorer.Navigation;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Explorer.Items;

/// <summary>What the nodes drawing an explorer's folder tree may ask it to do.</summary>
public interface IExplorerSidebarInteraction
{
    /// <summary>Shows what the folder holds, or hides it when it is already shown.</summary>
    void ToggleExpanded(string folderKey);

    /// <summary>Browses the folder, or the top level when none is named.</summary>
    Task SelectFolderAsync(ICatalogFolderItem? item);

    /// <summary>Asks for a new folder under the parent, or at the top level when none is named.</summary>
    Task CreateFolderAsync(ICatalogFolderItem? parent);

    /// <summary>Asks to rename the folder.</summary>
    Task RenameFolderAsync(ICatalogFolderItem item);

    /// <summary>Asks to delete the folder.</summary>
    Task DeleteFolderAsync(ICatalogFolderItem item);

    /// <summary>Reports what was dropped onto the folder.</summary>
    Task DropOnFolderAsync(ICatalogFolderItem? item);

    /// <summary>Records the folder as the one being dragged.</summary>
    Task BeginFolderDragAsync(ICatalogFolderItem item);

    /// <summary>Adds the folder to the selection, or removes it when it is already selected.</summary>
    Task ToggleFolderSelectAsync(ICatalogFolderItem item);
}
