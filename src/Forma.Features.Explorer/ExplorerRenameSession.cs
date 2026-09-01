namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>The one rename in progress, of a folder or any other resource, with the draft name being typed.</summary>
public sealed class ExplorerRenameSession
{
    /// <summary>What is being renamed, or null while nothing is.</summary>
    public ResourceInfo? Target { get; private set; }

    /// <summary>The name as typed so far.</summary>
    public string DraftName { get; set; } = string.Empty;

    /// <summary>The target when it is a folder.</summary>
    public FolderResourceInfo? Folder
        => Target as FolderResourceInfo;

    /// <summary>The target when it is anything but a folder.</summary>
    public ResourceInfo? Resource
        => Target is FolderResourceInfo ? null : Target;

    /// <summary>Whether the rename holds a target and a usable name.</summary>
    public bool CanConfirm
        => Target is not null && !string.IsNullOrWhiteSpace(DraftName);

    /// <summary>The draft name as a confirmation would apply it.</summary>
    public string ConfirmedName
        => DraftName.Trim();

    /// <summary>Starts renaming the given target, replacing any rename already in progress.</summary>
    public void BeginRenaming(ResourceInfo target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Target = target;
        DraftName = target.Name;
    }

    /// <summary>Ends the rename without applying it.</summary>
    public void EndRenaming()
    {
        Target = null;
        DraftName = string.Empty;
    }
}
