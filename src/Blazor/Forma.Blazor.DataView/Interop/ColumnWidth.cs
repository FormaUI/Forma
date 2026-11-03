namespace Forma.Blazor.Components.DataView.Interop;

using System.Diagnostics.CodeAnalysis;

/// <summary>The width one column wants: the inline size its header cell takes when the table is laid out at its content's own width.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record ColumnWidth
{
    /// <summary>The value the header cell names its column by.</summary>
    public string Key { get; init; } = "";

    /// <summary>The width the column wants, in pixels.</summary>
    public double Width { get; init; }
}
