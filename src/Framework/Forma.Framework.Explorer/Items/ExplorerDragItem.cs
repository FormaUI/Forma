namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Catalog;

/// <summary>The item currently being dragged.</summary>
public sealed class ExplorerDragItem
{
    /// <summary>The item being dragged.</summary>
    public ICatalogItem? Target { get; private set; }

    /// <summary>Starts dragging the target.</summary>
    public void Begin(ICatalogItem target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Target = target;
    }

    /// <summary>Ends the drag.</summary>
    public void End()
        => Target = null;
}
