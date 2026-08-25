namespace Forma.UI.Features.Toolbar;

using Forma.UI.Style.Glyphs;

/// <summary>One option inside a <see cref="FormaToolbarItem"/>'s menu.</summary>
public sealed record FormaToolbarOption
{
    /// <summary>The unique identifier of the option within its item.</summary>
    public required string Id { get; init; }

    /// <summary>The option's label and accessible name, localized by the host.</summary>
    public required string Title { get; init; }

    /// <summary>The icon shown beside the option, or null when its title stands alone.</summary>
    public Glyph? Icon { get; init; }

    /// <summary>How the icon is drawn.</summary>
    public GlyphStyle IconStyle { get; init; } = GlyphStyle.Regular;

    /// <summary>The command invoked when the option is picked.</summary>
    public required string Command { get; init; }

    /// <summary>Whether this is the option the item's current state matches.</summary>
    public bool IsActive { get; init; }

    /// <summary>Inline CSS previewing the option's effect on its own label, or null.</summary>
    public string? PreviewStyle { get; init; }

    /// <summary>A CSS color rendered as a swatch beside the label, or null.</summary>
    public string? Swatch { get; init; }
}
