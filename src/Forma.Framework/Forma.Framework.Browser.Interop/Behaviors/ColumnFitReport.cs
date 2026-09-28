namespace Forma.Framework.Browser.Interop.Behaviors;

/// <summary>What a column fit reports about its table: the width it has and the width each of its columns wants.</summary>
/// <param name="AvailableWidth">The inline size the table's container gives it, in pixels.</param>
/// <param name="Columns">The width each header cell that names a column wants, in the order the cells stand in.</param>
public sealed record ColumnFitReport(double AvailableWidth, IReadOnlyList<ColumnWidth> Columns);
