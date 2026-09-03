namespace Forma.Blazor.Components.Inputs;

/// <summary>How the emoji picker browses: its search box, its category tabs, and the recents it keeps.</summary>
public sealed record EmojiPickerOptions
{
    /// <summary>How many recently chosen emoji to keep.</summary>
    public int MaxRecent { get; init; } = 24;

    /// <summary>Whether the search box is shown.</summary>
    public bool ShowSearch { get; init; } = true;

    /// <summary>Whether the category tab strip is shown.</summary>
    public bool ShowGroupTabs { get; init; } = true;

    /// <summary>The search box's configuration; a null placeholder takes the picker's own.</summary>
    public SearchBoxOptions Search { get; init; } = new() { DebounceDelay = TimeSpan.FromMilliseconds(200) };

    /// <summary>Options showing the search box and every category tab, keeping two dozen recents.</summary>
    public static EmojiPickerOptions Default { get; } = new();
}
