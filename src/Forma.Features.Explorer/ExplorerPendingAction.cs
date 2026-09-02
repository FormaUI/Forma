namespace Forma.Features.Explorer;

/// <summary>An explorer action currently awaiting confirmation, as components read it.</summary>
public abstract class ExplorerPendingAction
{
    /// <summary>Whether the action has everything required for confirmation.</summary>
    public abstract bool CanConfirm { get; }

    /// <summary>Clears the pending action.</summary>
    protected internal abstract void Clear();
}
