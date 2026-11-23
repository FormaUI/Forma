namespace Forma.Blazor.Components.Charts;

/// <summary>A named series of values over time, drawn in the color and the marks of its place among a chart's series.</summary>
/// <param name="Name">The series' name, as the legend, the readout and the table write it.</param>
/// <param name="Points">The series' points.</param>
public sealed record ChartSeries(string Name, IReadOnlyList<ChartPoint> Points);
