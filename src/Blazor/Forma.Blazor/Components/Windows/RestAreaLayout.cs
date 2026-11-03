namespace Forma.Blazor.Components.Windows;

/// <summary>How a <see cref="FormaRestArea"/> arranges its handles.</summary>
public enum RestAreaLayout
{
    /// <summary>Along a row, wrapping when the row is full.</summary>
    Row,

    /// <summary>Down a column.</summary>
    Column,

    /// <summary>In a grid of equal columns, as many as fit.</summary>
    Grid,
}
