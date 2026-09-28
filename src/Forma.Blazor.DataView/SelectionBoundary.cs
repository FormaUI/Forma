namespace Forma.Blazor.Components.DataView;

/// <summary>A bound on how many items a <see cref="FormaDataView{TItem}"/> may have selected.</summary>
public enum SelectionBoundary
{
    /// <summary>The fewest items that must remain selected.</summary>
    Minimum,

    /// <summary>The most items that may be selected at once.</summary>
    Maximum,
}
