namespace Forma.Framework.Explorer.Actions;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Items;

/// <summary>An item being renamed, and its draft name.</summary>
public sealed class ExplorerRenameAction<TTarget> : ExplorerNamedAction<TTarget>
    where TTarget : class, ICatalogItem
{
    /// <summary>The item being renamed.</summary>
    public TTarget? Target => Item;

    /// <summary>Starts renaming the target, with its current name as the draft.</summary>
    public void Request(TTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Set(target, target.Name);
    }
}
