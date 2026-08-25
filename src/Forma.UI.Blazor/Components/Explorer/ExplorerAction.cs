namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Resources;
using Forma.UI.Style.Icons;

/// <summary>One operation an explorer offers on a single media resource.</summary>
public sealed record ExplorerAction
{
    /// <summary>Stable identifier, rendered as a data attribute so callers can target the action.</summary>
    public required string Id { get; init; }

    /// <summary>The glyph rendered inside the action's button.</summary>
    public required Icon Icon { get; init; }

    /// <summary>Produces the localized, resource-independent label shown for the action.</summary>
    public required Func<string> Text { get; init; }

    /// <summary>Runs the operation against a resource.</summary>
    public required Func<ResourceInfo, Task> Invoke { get; init; }

    /// <summary>Whether the operation destroys data and reads as destructive.</summary>
    public bool IsDestructive { get; init; }
}
