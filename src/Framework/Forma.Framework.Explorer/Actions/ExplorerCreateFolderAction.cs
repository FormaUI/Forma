namespace Forma.Framework.Explorer.Actions;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Items;

/// <summary>A new folder being named under its parent.</summary>
public sealed class ExplorerCreateFolderAction : ExplorerNamedAction<ICatalogFolderItem>
{
    /// <summary>The parent of the new folder, or null for the top level.</summary>
    public ICatalogFolderItem? Parent => Item;

    /// <summary>Starts a new folder draft under the parent, or at the top level when null.</summary>
    public void Request(ICatalogFolderItem? parent)
    {
        if (parent is null)
        {
            Set();
            return;
        }

        Set(parent);
    }
}
