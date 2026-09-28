namespace Forma.Blazor.Components.DataView;

/// <summary>Caption configuration for a <see cref="FormaDataView{TItem}"/>'s checked-selection master toggle.</summary>
public sealed record CheckedSelectionCaption
{
    /// <summary>Hides the master control while leaving every item's box visible.</summary>
    public static CheckedSelectionCaption Hidden { get; } = new();

    /// <summary>Text shown while the control can select every eligible item.</summary>
    public string? SelectAllText { get; init; }

    /// <summary>Text shown while the control can clear the current selection.</summary>
    public string? DeselectAllText { get; init; }
}
