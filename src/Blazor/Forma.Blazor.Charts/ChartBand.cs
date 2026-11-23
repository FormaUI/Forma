namespace Forma.Blazor.Components.Charts;

/// <summary>The band a line chart fills between two of its series, such as a median and a 95th percentile.</summary>
/// <param name="LowerSeriesIndex">The index among the chart's series of the series bounding the band from below.</param>
/// <param name="UpperSeriesIndex">The index among the chart's series of the series bounding the band from above.</param>
public sealed record ChartBand(int LowerSeriesIndex, int UpperSeriesIndex);
