namespace Forma.UI.Features.Composer.Plugins;

using Forma.UI.Style.Glyphs;

/// <summary>A toolbar entry a plugin contributes to the composer.</summary>
public sealed record ComposerToolbarItem
{
    /// <summary>The unique identifier of the toolbar item.</summary>
    public required string Id { get; init; }

    /// <summary>The item's label, in English, used as the localization key and accessible name.</summary>
    public required string Title { get; init; }

    /// <summary>The icon shown on the item, or null when custom markup renders it.</summary>
    public Glyph? Icon { get; init; }

    /// <summary>The toolbar group the item is listed under.</summary>
    public string? Group { get; init; }

    /// <summary>The identifier of the row the item is laid out in, or null for its contributor's default row.</summary>
    public string? Row { get; init; }

    /// <summary>The command invoked when the item is activated.</summary>
    public string? Command { get; init; }

    /// <summary>A component rendered in place of a standard toolbar button, or null.</summary>
    public Type? CustomComponent { get; init; }

    /// <summary>The options the item offers as a menu, or empty when the item is a plain command.</summary>
    public IReadOnlyList<ComposerToolbarOption> Options { get; init; } = [];

    /// <summary>Whether the item reflects an on/off state rather than firing once.</summary>
    public bool IsToggle { get; init; }
}
