namespace Forma.Framework.Toolbar;

using Forma.Framework.Icons;

/// <summary>An option a <see cref="ToolbarMenu"/> presents.</summary>
public sealed record ToolbarOption
{
    /// <summary>The unique identifier of the option within its item.</summary>
    public required string Id { get; init; }

    /// <summary>The option's label and accessible name, localized by the host.</summary>
    public required string Title { get; init; }

    /// <summary>The icon shown beside the option.</summary>
    public Icon? Icon { get; init; }

    /// <summary>The command invoked when the option is picked.</summary>
    public required string Command { get; init; }

    /// <summary>Whether the option matches the menu's current state.</summary>
    public bool IsActive { get; init; }

    /// <summary>Inline CSS previewing the option's effect on its own label.</summary>
    public string? PreviewStyle { get; init; }

    /// <summary>A CSS color rendered as a swatch beside the label.</summary>
    public string? Swatch { get; init; }
}
