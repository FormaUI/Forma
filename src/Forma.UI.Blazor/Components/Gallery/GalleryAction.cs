namespace Forma.UI.Blazor.Components.Gallery;

using Forma.Framework.Resources;
using Forma.UI.Style.Glyphs;

/// <summary>One operation a gallery offers on a single media resource.</summary>
public sealed record GalleryAction
{
    /// <summary>Stable identifier, rendered as a data attribute so callers can target the action.</summary>
    public required string Id { get; init; }

    /// <summary>The glyph rendered inside the action's button.</summary>
    public required Glyph Icon { get; init; }

    /// <summary>Produces the localized accessible name for the resource the action would run on.</summary>
    public required Func<ResourceInfo, string> Label { get; init; }

    /// <summary>Runs the operation against a resource.</summary>
    public required Func<ResourceInfo, Task> Invoke { get; init; }

    /// <summary>Whether the operation destroys data and reads as destructive.</summary>
    public bool IsDestructive { get; init; }
}
