namespace Forma.Blazor.Components.Admin.Data;

/// <summary>A bound on how many rows a <see cref="FormaDataGrid{TGridItem}"/> may have selected.</summary>
public enum SelectionBoundary
{
    /// <summary>The fewest rows that must remain selected.</summary>
    Minimum,

    /// <summary>The most rows that may be selected at once.</summary>
    Maximum,
}
