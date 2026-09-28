namespace Forma.Framework.Browser.Interop.Behaviors;

/// <summary>The width one column wants: the inline size its header cell takes when the table is laid out at its content's own width.</summary>
/// <param name="Key">The value the header cell names its column by.</param>
/// <param name="Width">The width the column wants, in pixels.</param>
public sealed record ColumnWidth(string Key, double Width);
