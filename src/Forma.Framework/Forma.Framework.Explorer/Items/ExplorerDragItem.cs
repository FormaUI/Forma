namespace Forma.Framework.Explorer.Items;

/// <summary>The item currently being dragged.</summary>
public sealed class ExplorerDragItem
{
    /// <summary>The item being dragged, or null when nothing is.</summary>
    public IExplorerItem? Target { get; private set; }

    /// <summary>Starts dragging the given item.</summary>
    public void Begin(IExplorerItem target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Target = target;
    }

    /// <summary>Ends the drag, wherever it lands.</summary>
    public void End()
        => Target = null;
}
