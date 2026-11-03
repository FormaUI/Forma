namespace Forma.Blazor.Components.DataView.Interop;

using System.Diagnostics.CodeAnalysis;

/// <summary>What a column fit reports about its table: the width it has and the width each of its columns wants.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record ColumnFitReport
{
    /// <summary>The inline size the table's container gives it, in pixels.</summary>
    public double AvailableWidth { get; init; }

    /// <summary>The width each header cell that names a column wants, in the order the cells stand in.</summary>
    public IReadOnlyList<ColumnWidth> Columns { get; init; } = [];
}
