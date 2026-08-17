namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

/// <summary>One operation a gallery offers on a single media item.</summary>
public sealed record GalleryAction
{
    /// <summary>Stable identifier, rendered as a data attribute so callers can target the action.</summary>
    public required string Id { get; init; }

    /// <summary>The glyph class rendered inside the action's button.</summary>
    public required string Icon { get; init; }

    /// <summary>Produces the localized accessible name for the item the action would run on.</summary>
    public required Func<ResourceInfo, string> Label { get; init; }

    /// <summary>Runs the operation against an item.</summary>
    public required Func<ResourceInfo, Task> Invoke { get; init; }

    /// <summary>Whether the operation destroys data and reads as destructive.</summary>
    public bool IsDestructive { get; init; }
}
