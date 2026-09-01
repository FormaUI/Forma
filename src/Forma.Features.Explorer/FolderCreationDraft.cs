namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>A folder being named before it exists, under the parent it will land in.</summary>
public sealed class FolderCreationDraft
{
    /// <summary>Whether the draft is being edited.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The folder the new one lands in, or null for the top level.</summary>
    public FolderResourceInfo? Parent { get; private set; }

    /// <summary>The name as typed so far.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the draft holds a usable name.</summary>
    public bool CanCreate
        => IsActive && !string.IsNullOrWhiteSpace(Name);

    /// <summary>The draft name as a confirmation would apply it.</summary>
    public string ConfirmedName
        => Name.Trim();

    /// <summary>Starts a fresh draft under the given parent.</summary>
    public void BeginCreating(FolderResourceInfo? parent)
    {
        Parent = parent;
        Name = string.Empty;
        IsActive = true;
    }

    /// <summary>Ends the draft without creating anything.</summary>
    public void EndCreating()
    {
        IsActive = false;
        Parent = null;
        Name = string.Empty;
    }
}
