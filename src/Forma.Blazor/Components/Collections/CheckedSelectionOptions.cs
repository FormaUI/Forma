namespace Forma.Blazor.Components.Collections;

/// <summary>The bounds of a checked selection, and the caption its header offers.</summary>
public sealed record CheckedSelectionOptions
{
    /// <summary>The fewest rows that must remain selected once a selection exists. A selected row cannot be individually unchecked below this floor; clearing the whole selection is always allowed.</summary>
    public int? Minimum { get; init; }

    /// <summary>The most rows that may be selected at once.</summary>
    public int? Maximum { get; init; }

    /// <summary>Caption shown in place of the header checkbox. A <see langword="null"/> value keeps the plain checkbox.</summary>
    public CheckedSelectionCaption? Caption { get; init; }

    /// <summary>Options with no bounds and the plain header checkbox.</summary>
    public static CheckedSelectionOptions Default { get; } = new();
}
