namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>A folder being named before it exists, under the parent it will land in.</summary>
public sealed class ExplorerCreateFolderAction : ExplorerNamedAction<FolderResourceInfo>
{
    /// <summary>The folder the new one lands in, or null for the top level.</summary>
    public FolderResourceInfo? Parent => Resource;

    /// <summary>Starts a fresh draft under the given parent, or the top level when null.</summary>
    public void Request(FolderResourceInfo? parent)
    {
        if (parent is null)
        {
            Set();
            return;
        }

        Set(parent);
    }
}
