namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>What is currently being dragged, a folder or a resource, and never both.</summary>
public sealed class ExplorerDrag
{
    /// <summary>The folder or resource being dragged, or null when nothing is.</summary>
    public ResourceInfo? Target { get; private set; }

    /// <summary>Target as a folder, when that is what is being dragged.</summary>
    public FolderResourceInfo? Folder => Target as FolderResourceInfo;

    /// <summary>Target as a resource, when it is not a folder.</summary>
    public ResourceInfo? Resource => Target is FolderResourceInfo ? null : Target;

    /// <summary>Starts dragging the given folder or resource.</summary>
    public void Begin(ResourceInfo target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Target = target;
    }

    /// <summary>Ends the drag, wherever it lands.</summary>
    public void End()
        => Target = null;
}
