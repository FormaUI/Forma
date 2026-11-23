namespace Forma.Blazor.Components.Charts;

/// <summary>A series' value at a time, which stands in the chart's period holding the time, or on a span at the time itself.</summary>
/// <param name="Time">The time the value was measured at.</param>
/// <param name="Value">The value, where null is a period with no data.</param>
public readonly record struct ChartPoint(DateTimeOffset Time, double? Value)
{
    /// <summary>The kind of event the point records, which draws the point in the mark's shape and names the mark beside its value.</summary>
    public ChartMark? Mark { get; init; }

    /// <summary>The note the readout and the table give beside the point's value.</summary>
    public string? Note { get; init; }
}
