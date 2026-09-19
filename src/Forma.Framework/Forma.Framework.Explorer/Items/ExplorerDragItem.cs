namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Catalog;

/// <summary>The item currently being dragged.</summary>
public sealed class ExplorerDragItem
{
    /// <summary>The item being dragged, or null when nothing is.</summary>
    public ICatalogItem? Target { get; private set; }

    /// <summary>Starts dragging the given item.</summary>
    public void Begin(ICatalogItem target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Target = target;
    }

    /// <summary>Ends the drag, wherever it lands.</summary>
    public void End()
        => Target = null;
}
