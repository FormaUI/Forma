namespace Forma.Blazor.Components.Charts;

/// <summary>The rules every chart's parameters keep.</summary>
internal static class ChartValidation
{
    /// <summary>The most series a chart draws, as many as its colors, dashes and markers tell apart.</summary>
    public const int MaximumSeriesCount = 4;

    /// <summary>Checks a chart's title, its one to four named series, and its threshold's label.</summary>
    public static void Validate(string title, IReadOnlyList<ChartSeries> series, ChartThreshold? threshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, "Title");
        ArgumentNullException.ThrowIfNull(series, "Series");
        ArgumentOutOfRangeException.ThrowIfZero(series.Count, "Series");
        ArgumentOutOfRangeException.ThrowIfGreaterThan(series.Count, MaximumSeriesCount, "Series");

        foreach (var entry in series)
        {
            ArgumentNullException.ThrowIfNull(entry, "Series");
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.Name, "Series");
            ArgumentNullException.ThrowIfNull(entry.Points, "Series");
        }

        if (threshold is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(threshold.Label, "Threshold");
        }
    }
}
