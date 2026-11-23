namespace Forma.Blazor.Components.Charts;

using System.Globalization;

/// <summary>The rules every chart's parameters keep.</summary>
internal static class ChartValidation
{
    /// <summary>The most series a chart draws, as many as its colors, dashes and markers tell apart.</summary>
    public const int MaximumSeriesCount = 4;

    /// <summary>The most marks a chart's points carry, as many as its marks' shapes tell apart.</summary>
    public const int MaximumMarkCount = 4;

    /// <summary>Checks a chart's title, its one to four named series and their values, and its threshold.</summary>
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

            foreach (var point in entry.Points)
            {
                if (point.Value is { } value && !double.IsFinite(value))
                {
                    throw new ArgumentException(
                        string.Create(CultureInfo.InvariantCulture, $"A point's value is a finite number, or null for no data; the point of {entry.Name} at {point.Time:O} holds {value}."),
                        nameof(series));
                }
            }
        }

        if (series.SelectMany(entry => entry.Points).Select(point => point.Mark).OfType<ChartMark>().Distinct().Count() > MaximumMarkCount)
        {
            throw new ArgumentException($"A chart's points carry at most {MaximumMarkCount} marks, as many as their shapes tell apart.", nameof(series));
        }

        if (threshold is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(threshold.Label, "Threshold");

            if (!double.IsFinite(threshold.Value))
            {
                throw new ArgumentException("A threshold's value is a finite number.", nameof(threshold));
            }
        }
    }

    /// <summary>Checks that a start belongs to a span, and that a span ends no earlier than it starts.</summary>
    public static void ValidateSpan(ChartRange range, DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start is not null && range is not ChartRange.Span)
        {
            throw new ArgumentException("A chart starts where its fixed range does, so a start belongs to a span alone.", nameof(start));
        }

        if (start is { } spanStart && end is { } spanEnd && spanEnd < spanStart)
        {
            throw new ArgumentException("A span ends no earlier than it starts.", nameof(end));
        }
    }
}
