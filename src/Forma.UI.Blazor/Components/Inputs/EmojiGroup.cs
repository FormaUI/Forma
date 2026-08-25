namespace Forma.UI.Blazor.Components.Inputs;

using Forma.UI.Style.Icons;

/// <summary>One category of emoji, as the picker's tab strip presents it.</summary>
public sealed record EmojiGroup
{
    /// <summary>The identifier the catalogue's entries name this group by.</summary>
    public required string Id { get; init; }

    /// <summary>The English source string naming this group, used as its localization key.</summary>
    public required string Title { get; init; }

    /// <summary>The glyph standing for this group on the tab strip.</summary>
    public required Icon Icon { get; init; }
}
