namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>The one deletion awaiting confirmation, of either resources or folders.</summary>
public sealed class ExplorerDeletion
{
    /// <summary>The resources awaiting confirmation, empty while folders or nothing await it.</summary>
    public IReadOnlyList<ResourceInfo> Resources { get; private set; } = [];

    /// <summary>The folders awaiting confirmation, empty while resources or nothing await it.</summary>
    public IReadOnlyList<FolderResourceInfo> Folders { get; private set; } = [];

    /// <summary>Asks confirmation for deleting the given resources, replacing any deletion already pending.</summary>
    public void Request(IReadOnlyList<ResourceInfo> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        Resources = [.. resources];
        Folders = [];
    }

    /// <summary>Asks confirmation for deleting the given folders, replacing any deletion already pending.</summary>
    public void Request(IReadOnlyList<FolderResourceInfo> folders)
    {
        ArgumentNullException.ThrowIfNull(folders);

        Folders = [.. folders];
        Resources = [];
    }

    /// <summary>Drops the pending deletion, confirmed or not.</summary>
    public void Clear()
    {
        Resources = [];
        Folders = [];
    }
}
