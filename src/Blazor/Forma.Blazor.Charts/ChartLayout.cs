namespace Forma.Blazor.Components.Charts;

using System.Globalization;

/// <summary>What a chart draws: its periods, each series' value in each period, the value scale, and the labels of both axes in the chart's culture.</summary>
internal sealed class ChartLayout
{
    private readonly CultureInfo _culture;
    private readonly string _valueFormat;

    private ChartLayout(ChartTimeline timeline, double?[][] values, ChartScale scale, CultureInfo culture, string valueFormat, ChartThreshold? threshold)
    {
        Timeline = timeline;
        Values = values;
        Scale = scale;
        Threshold = threshold;
        _culture = culture;
        _valueFormat = valueFormat;
        PeriodTexts = [.. timeline.LocalStarts.Select(FormatPeriod)];
        TimeLabels = CreateTimeLabels();
    }

    /// <summary>The chart's periods.</summary>
    public ChartTimeline Timeline { get; }

    /// <summary>Each series' value in each period, by series and then by period, null for a period with no data.</summary>
    public IReadOnlyList<double?[]> Values { get; }

    /// <summary>The chart's value scale.</summary>
    public ChartScale Scale { get; }

    /// <summary>The value the chart marks with a labelled line.</summary>
    public ChartThreshold? Threshold { get; }

    /// <summary>Each period written in full, as its readout and its row in the table name it.</summary>
    public IReadOnlyList<string> PeriodTexts { get; }

    /// <summary>The labels of the time axis.</summary>
    public IReadOnlyList<ChartTimeLabel> TimeLabels { get; }

    /// <summary>Whether any series holds a value in any period.</summary>
    public bool HasData
        => Values.Any(series => series.Any(value => value is not null));

    /// <summary>Lays out <paramref name="series"/> over <paramref name="range"/>, ending with the period that holds <paramref name="end"/> on the clock of <paramref name="timeZone"/>, its values written by <paramref name="valueFormat"/> and its labels in <paramref name="culture"/>.</summary>
    public static ChartLayout Create(IReadOnlyList<ChartSeries> series, ChartRange range, DateTimeOffset end, TimeZoneInfo timeZone, CultureInfo culture, string valueFormat, ChartThreshold? threshold)
    {
        var timeline = ChartTimeline.Create(range, end, timeZone);
        var values = series.Select(entry => PlacePoints(entry, timeline)).ToArray();
        var present = values.SelectMany(entry => entry).OfType<double>().ToList();

        if (threshold is not null)
        {
            present.Add(threshold.Value);
        }

        var scale = ChartScale.Create(present.Count > 0 ? present.Min() : 0, present.Count > 0 ? present.Max() : 0);

        return new ChartLayout(timeline, values, scale, culture, valueFormat, threshold);
    }

    /// <summary>Where the middle of the period at <paramref name="index"/> stands along the plot, from 0 at its inline start to 1 at its end.</summary>
    public double GetCenter(int index)
        => (index + 0.5) / Timeline.Count;

    /// <summary>Writes <paramref name="value"/> by the chart's format in its culture.</summary>
    public string FormatValue(double value)
        => value.ToString(_valueFormat, _culture);

    /// <summary>The runs of two or more periods in which the series at <paramref name="seriesIndex"/> holds a value, each its first and last period's indexes, which a line joins.</summary>
    public IEnumerable<(int First, int Last)> GetLineRuns(int seriesIndex)
        => GetRuns(index => Values[seriesIndex][index] is not null);

    /// <summary>The runs of two or more periods in which both the series at <paramref name="lowerIndex"/> and at <paramref name="upperIndex"/> hold a value, which a band fills.</summary>
    public IEnumerable<(int First, int Last)> GetBandRuns(int lowerIndex, int upperIndex)
        => GetRuns(index => Values[lowerIndex][index] is not null && Values[upperIndex][index] is not null);

    /// <summary>Whether the point of the series at <paramref name="seriesIndex"/> in the period at <paramref name="index"/> draws its marker whatever period is read: a point standing alone between gaps, or the series' last.</summary>
    public bool IsStanding(int seriesIndex, int index)
    {
        var series = Values[seriesIndex];

        if (series[index] is null)
        {
            return false;
        }

        var isAlone = (index == 0 || series[index - 1] is null) && (index == series.Length - 1 || series[index + 1] is null);
        var isLast = Array.FindLastIndex(series, value => value is not null) == index;

        return isAlone || isLast;
    }

    // A series' points, each in the period holding its time, the last listed where several fall in one period.
    private static double?[] PlacePoints(ChartSeries series, ChartTimeline timeline)
    {
        var values = new double?[timeline.Count];

        foreach (var point in series.Points)
        {
            if (timeline.FindIndex(point.Time) is var index and >= 0)
            {
                values[index] = point.Value;
            }
        }

        return values;
    }

    private IEnumerable<(int First, int Last)> GetRuns(Func<int, bool> isPresent)
    {
        var first = -1;

        for (var index = 0; index <= Timeline.Count; index++)
        {
            var isInRun = index < Timeline.Count && isPresent(index);

            if (isInRun && first < 0)
            {
                first = index;
            }
            else if (!isInRun && first >= 0)
            {
                if (index - 1 > first)
                {
                    yield return (first, index - 1);
                }

                first = -1;
            }
        }
    }

    private string FormatPeriod(DateTime localStart)
        => Timeline.Range switch
        {
            ChartRange.Hour or ChartRange.Day => localStart.ToString("g", _culture),
            ChartRange.ThirtyDays => localStart.ToString("d", _culture),
            _ => localStart.ToString(_culture.DateTimeFormat.YearMonthPattern, _culture),
        };

    // A label stands at the middle of the period it names. The labels of tier 0 always show and those of tiers 1 and 2 as the chart
    // widens; minutes and hours are labelled on round marks of the zone's clock, days and months counting back from the last.
    private List<ChartTimeLabel> CreateTimeLabels()
    {
        var labels = new List<ChartTimeLabel>();
        var count = Timeline.Count;

        for (var index = 0; index < count; index++)
        {
            var localStart = Timeline.LocalStarts[index];
            var laterPeriodCount = count - 1 - index;
            int? tier = Timeline.Range switch
            {
                ChartRange.Hour => localStart.Minute % 30 == 0 ? 0 : localStart.Minute % 15 == 0 ? 1 : localStart.Minute % 10 == 0 ? 2 : null,
                ChartRange.Day => localStart.Hour % 12 == 0 ? 0 : localStart.Hour % 6 == 0 ? 1 : localStart.Hour % 3 == 0 ? 2 : null,
                ChartRange.ThirtyDays => laterPeriodCount % 14 == 0 ? 0 : laterPeriodCount % 7 == 0 ? 1 : laterPeriodCount % 7 == 3 ? 2 : null,
                _ => laterPeriodCount % 6 == 0 ? 0 : laterPeriodCount % 3 == 0 ? 1 : 2,
            };

            if (tier is { } labelTier)
            {
                labels.Add(new ChartTimeLabel(index, FormatTimeLabel(localStart), labelTier));
            }
        }

        return labels;
    }

    private string FormatTimeLabel(DateTime localStart)
        => Timeline.Range switch
        {
            ChartRange.Hour or ChartRange.Day => localStart.ToString("t", _culture),
            ChartRange.ThirtyDays => localStart.ToString(_culture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM", StringComparison.Ordinal), _culture),
            _ => localStart.ToString("MMM", _culture),
        };
}
