namespace Forma.Features.Explorer.Actions;

/// <summary>A pending explorer action that collects a name.</summary>
public abstract class ExplorerPendingNamedAction : ExplorerPendingAction
{
    /// <summary>The current draft name.</summary>
    public abstract string Name { get; protected internal set; }
}
