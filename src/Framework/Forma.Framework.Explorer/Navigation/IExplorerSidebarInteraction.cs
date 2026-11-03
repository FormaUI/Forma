namespace Forma.Framework.Explorer.Navigation;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Explorer.Items;

/// <summary>What the nodes drawing an explorer's folder tree may ask it to do.</summary>
public interface IExplorerSidebarInteraction
{
    /// <summary>Toggles whether the folder shows what it holds.</summary>
    void ToggleExpanded(string folderKey);

    /// <summary>Browses the folder, or the top level when null.</summary>
    Task SelectFolderAsync(ICatalogFolderItem? item);

    /// <summary>Asks for a new folder under the parent, or at the top level when null.</summary>
    Task CreateFolderAsync(ICatalogFolderItem? parent);

    /// <summary>Asks to rename the folder.</summary>
    Task RenameFolderAsync(ICatalogFolderItem item);

    /// <summary>Asks to delete the folder.</summary>
    Task DeleteFolderAsync(ICatalogFolderItem item);

    /// <summary>Starts dragging the folder.</summary>
    Task BeginFolderDragAsync(ICatalogFolderItem item);

    /// <summary>Toggles whether the folder is selected.</summary>
    Task ToggleFolderSelectAsync(ICatalogFolderItem item);
}
