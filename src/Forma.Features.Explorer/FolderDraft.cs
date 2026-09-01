namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>A folder being named before it exists, under the parent it will land in.</summary>
public sealed class FolderDraft
{
    /// <summary>Whether the draft is being edited.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The folder the new one lands in, or null for the top level.</summary>
    public FolderResourceInfo? Parent { get; private set; }

    /// <summary>The name as typed so far.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the draft holds a usable name.</summary>
    public bool CanCreate
        => IsOpen && !string.IsNullOrWhiteSpace(Name);

    /// <summary>The draft name as a confirmation would apply it.</summary>
    public string ConfirmedName
        => Name.Trim();

    /// <summary>Starts a fresh draft under the given parent.</summary>
    public void Open(FolderResourceInfo? parent)
    {
        Parent = parent;
        Name = string.Empty;
        IsOpen = true;
    }

    /// <summary>Ends the draft without creating anything.</summary>
    public void Close()
    {
        IsOpen = false;
        Parent = null;
        Name = string.Empty;
    }
}
