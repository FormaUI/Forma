namespace Forma.Blazor.Components.DataView;

/// <summary>The bounds of a checked selection, and the caption its header offers.</summary>
public sealed record CheckedSelectionOptions
{
    /// <summary>The fewest items that must remain selected once a selection exists.</summary>
    /// <remarks>Unchecking a single item stops at this floor; clearing the whole selection passes it.</remarks>
    public int? Minimum { get; init; }

    /// <summary>The most items that may be selected at once.</summary>
    public int? Maximum { get; init; }

    /// <summary>The caption shown in place of the header checkbox, where null or a caption with no text keeps the plain checkbox.</summary>
    public CheckedSelectionCaption? Caption { get; init; }

    /// <summary>Whether a press on an item checks or unchecks its box rather than activating the item, as a selection mode does, Enter and a press on an item that cannot be selected still activating it.</summary>
    public bool ChecksOnPress { get; init; }

    /// <summary>What the master control reaches when pressed.</summary>
    public SelectAllMode SelectAllMode { get; init; } = SelectAllMode.FromMenu;

    /// <summary>Options with no bounds and the plain header checkbox.</summary>
    public static CheckedSelectionOptions Default { get; } = new();
}
