namespace Forma.Blazor.Components.Data;

/// <summary>Details of a selection attempt blocked by a <see cref="FormaDataGrid{TGridItem}"/> selection bound.</summary>
public sealed record SelectionRejected
{
    /// <summary>Which bound blocked the attempt.</summary>
    public required SelectionBoundary Boundary { get; init; }

    /// <summary>The value of the bound that blocked the attempt.</summary>
    public required int Limit { get; init; }
}
