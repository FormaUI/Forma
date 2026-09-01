namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>The single resource being renamed, and its draft name.</summary>
public sealed class ExplorerRenameAction<TTarget> : ExplorerNamedAction<TTarget>
    where TTarget : ResourceInfo
{
    /// <summary>The resource being renamed, or null when nothing is.</summary>
    public TTarget? Target => Resource;

    /// <summary>Starts renaming the given resource, with its current name as the draft.</summary>
    public void Request(TTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Set(target, target.Name);
    }
}
