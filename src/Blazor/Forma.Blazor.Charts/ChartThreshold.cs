namespace Forma.Blazor.Components.Charts;

/// <summary>A value a chart marks with a labelled line across its plot, such as a target or a limit.</summary>
/// <param name="Value">The value the line stands at.</param>
/// <param name="Label">The line's label.</param>
public sealed record ChartThreshold(double Value, string Label);
