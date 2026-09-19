namespace Forma.Framework.Explorer.Actions;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Items;

/// <summary>A folder being named before it exists, under the parent it will land in.</summary>
public sealed class ExplorerCreateFolderAction : ExplorerNamedAction<ICatalogFolderItem>
{
    /// <summary>The folder the new one lands in, or null for the top level.</summary>
    public ICatalogFolderItem? Parent => Item;

    /// <summary>Starts a fresh draft under the given parent, or the top level when null.</summary>
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
