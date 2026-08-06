namespace Forma.Blazor.Components.Data;

/// <summary>Caption configuration for a <see cref="FormaDataGrid{TGridItem}"/>'s checked-selection master toggle.</summary>
public sealed record CheckedSelectionCaption
{
    /// <summary>Hides the master toggle while leaving row checkboxes visible.</summary>
    public static CheckedSelectionCaption Hidden { get; } = new();

    /// <summary>Text shown while the toggle can select every eligible row.</summary>
    public string? SelectAllText { get; init; }

    /// <summary>Text shown while the toggle can deselect the current selection.</summary>
    public string? DeselectAllText { get; init; }
}
