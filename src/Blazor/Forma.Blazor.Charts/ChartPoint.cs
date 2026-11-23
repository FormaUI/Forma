namespace Forma.Blazor.Components.Charts;

/// <summary>A series' value at a time, which stands in the chart's period holding the time.</summary>
/// <param name="Time">The time the value was measured at.</param>
/// <param name="Value">The value, where null is a period with no data.</param>
public readonly record struct ChartPoint(DateTimeOffset Time, double? Value);
