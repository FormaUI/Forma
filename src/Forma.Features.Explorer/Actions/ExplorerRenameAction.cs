namespace Forma.Features.Explorer.Actions;

using Forma.Features.Explorer.Items;

/// <summary>The single item being renamed, and its draft name.</summary>
public sealed class ExplorerRenameAction<TTarget> : ExplorerNamedAction<TTarget>
    where TTarget : class, IExplorerItem
{
    /// <summary>The item being renamed, or null when nothing is.</summary>
    public TTarget? Target => Item;

    /// <summary>Starts renaming the given item, with its current name as the draft.</summary>
    public void Request(TTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Set(target, target.Name);
    }
}
