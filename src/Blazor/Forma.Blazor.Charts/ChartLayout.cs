namespace Forma.Blazor.Components.Charts;

using System.Globalization;

/// <summary>What a chart draws: its slots, each series' point in each slot, the marks its points carry, the value scale, and the labels of both axes in the chart's culture.</summary>
internal sealed class ChartLayout
{
    // The steps a span's time axis can be labelled by, finest first, each a count of a unit of the zone's calendar.
    private static readonly (TimeUnit Unit, int Count)[] SpanSteps =
    [
        (TimeUnit.Minute, 1), (TimeUnit.Minute, 5), (TimeUnit.Minute, 10), (TimeUnit.Minute, 15), (TimeUnit.Minute, 30),
        (TimeUnit.Hour, 1), (TimeUnit.Hour, 3), (TimeUnit.Hour, 6), (TimeUnit.Hour, 12),
        (TimeUnit.Day, 1), (TimeUnit.Day, 2), (TimeUnit.Day, 7), (TimeUnit.Day, 14),
        (TimeUnit.Month, 1), (TimeUnit.Month, 3), (TimeUnit.Month, 6),
        (TimeUnit.Year, 1), (TimeUnit.Year, 2), (TimeUnit.Year, 5), (TimeUnit.Year, 10), (TimeUnit.Year, 25), (TimeUnit.Year, 50), (TimeUnit.Year, 100),
    ];

    private readonly CultureInfo _culture;
    private readonly string _valueFormat;
    private readonly List<ChartMark> _marks;

    private ChartLayout(ChartTimeline timeline, ChartPoint?[][] points, ChartScale scale, CultureInfo culture, string valueFormat, ChartThreshold? threshold)
    {
        Timeline = timeline;
        Points = points;
        Values = [.. points.Select(series => series.Select(point => point?.Value).ToArray())];
        _marks = [.. points.SelectMany(series => series).Select(point => point?.Mark).OfType<ChartMark>().Distinct()];
        Scale = scale;
        Threshold = threshold;
        _culture = culture;
        _valueFormat = valueFormat;
        PeriodTexts = [.. timeline.LocalStarts.Select(FormatPeriod)];
        TimeLabels = timeline.Range is ChartRange.Span ? CreateSpanLabels() : CreatePeriodLabels();
    }

    private enum TimeUnit
    {
        Minute,
        Hour,
        Day,
        Month,
        Year,
    }

    /// <summary>The chart's slots, its periods or its moments.</summary>
    public ChartTimeline Timeline { get; }

    /// <summary>Each series' point in each slot, by series and then by slot, null for a slot it holds none in.</summary>
    public IReadOnlyList<ChartPoint?[]> Points { get; }

    /// <summary>Each series' value in each slot, by series and then by slot, null for a slot with no data.</summary>
    public IReadOnlyList<double?[]> Values { get; }

    /// <summary>The marks the chart's points carry, in the order they first appear.</summary>
    public IReadOnlyList<ChartMark> Marks
        => _marks;

    /// <summary>The chart's value scale.</summary>
    public ChartScale Scale { get; }

    /// <summary>The value the chart marks with a labelled line.</summary>
    public ChartThreshold? Threshold { get; }

    /// <summary>Each slot written in full, as its readout and its row in the table name it.</summary>
    public IReadOnlyList<string> PeriodTexts { get; }

    /// <summary>The labels of the time axis.</summary>
    public IReadOnlyList<ChartTimeLabel> TimeLabels { get; }

    /// <summary>Whether any series holds a value in any slot.</summary>
    public bool HasData
        => Values.Any(series => series.Any(value => value is not null));

    /// <summary>
    /// Lays out <paramref name="series"/> over <paramref name="range"/> on the clock of <paramref name="timeZone"/>: a fixed range ending with
    /// the period that holds <paramref name="end"/>, or <paramref name="now"/> where it is null, or a span from <paramref name="start"/> to
    /// <paramref name="end"/>, its points' first and last times where either is null. Its values are written by <paramref name="valueFormat"/>
    /// and its labels in <paramref name="culture"/>, and its scale reaches zero where <paramref name="includesZero"/>.
    /// </summary>
    public static ChartLayout Create(IReadOnlyList<ChartSeries> series, ChartRange range, DateTimeOffset? start, DateTimeOffset? end, DateTimeOffset now, TimeZoneInfo timeZone, CultureInfo culture, string valueFormat, ChartThreshold? threshold, bool includesZero)
    {
        var timeline = range is ChartRange.Span ? CreateSpanTimeline(series, start, end, now, timeZone) : ChartTimeline.Create(range, end ?? now, timeZone);
        var points = series.Select(entry => PlacePoints(entry, timeline)).ToArray();
        var present = points.SelectMany(entry => entry).Select(point => point?.Value).OfType<double>().ToList();

        if (threshold is not null)
        {
            present.Add(threshold.Value);
        }

        var scale = ChartScale.Create(present.Count > 0 ? present.Min() : 0, present.Count > 0 ? present.Max() : 0, includesZero);

        return new ChartLayout(timeline, points, scale, culture, valueFormat, threshold);
    }

    /// <summary>Where the slot at <paramref name="index"/> stands along the plot, from 0 at its inline start to 1 at its end.</summary>
    public double GetCenter(int index)
        => Timeline.GetCenter(index);

    /// <summary>Where the baseline stands up the plot: zero, or the scale's edge nearer it where the scale doesn't reach zero.</summary>
    public double GetBaseline()
        => Scale.GetFraction(Math.Clamp(0, Scale.Minimum, Scale.Maximum));

    /// <summary>Writes <paramref name="value"/> by the chart's format in its culture.</summary>
    public string FormatValue(double value)
        => value.ToString(_valueFormat, _culture);

    /// <summary>The index among <see cref="Marks"/> of <paramref name="mark"/>.</summary>
    public int GetMarkIndex(ChartMark mark)
        => _marks.IndexOf(mark);

    /// <summary>The runs of two or more slots in which the series at <paramref name="seriesIndex"/> holds a value, each its first and last slot's indexes, which a line joins.</summary>
    public IEnumerable<(int First, int Last)> GetLineRuns(int seriesIndex)
        => GetRuns(index => Values[seriesIndex][index] is not null);

    /// <summary>The runs of two or more slots in which both the series at <paramref name="lowerIndex"/> and at <paramref name="upperIndex"/> hold a value, which a band fills.</summary>
    public IEnumerable<(int First, int Last)> GetBandRuns(int lowerIndex, int upperIndex)
        => GetRuns(index => Values[lowerIndex][index] is not null && Values[upperIndex][index] is not null);

    /// <summary>Whether the point of the series at <paramref name="seriesIndex"/> in the slot at <paramref name="index"/> draws its marker whatever slot is read: a point standing alone between gaps, a point carrying a mark, or the series' last.</summary>
    public bool IsStanding(int seriesIndex, int index)
    {
        var series = Values[seriesIndex];

        if (series[index] is null)
        {
            return false;
        }

        var isAlone = (index == 0 || series[index - 1] is null) && (index == series.Length - 1 || series[index + 1] is null);
        var isLast = Array.FindLastIndex(series, value => value is not null) == index;

        return isAlone || isLast || Points[seriesIndex][index]?.Mark is not null;
    }

    // A span from its start to its end, either taken from the earliest or the latest of its points' times, or now where it has none.
    private static ChartTimeline CreateSpanTimeline(IReadOnlyList<ChartSeries> series, DateTimeOffset? start, DateTimeOffset? end, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var times = series.SelectMany(entry => entry.Points).Select(point => point.Time).ToList();
        var spanStart = start ?? (times.Count > 0 ? times.Min() : end ?? now);
        var spanEnd = end ?? (times.Count > 0 ? times.Max() : spanStart);

        return ChartTimeline.CreateSpan(times, spanStart, spanEnd < spanStart ? spanStart : spanEnd, timeZone);
    }

    // A series' points, each in the slot holding its time, the last listed where several fall in one slot.
    private static ChartPoint?[] PlacePoints(ChartSeries series, ChartTimeline timeline)
    {
        var points = new ChartPoint?[timeline.Count];

        foreach (var point in series.Points)
        {
            if (timeline.FindIndex(point.Time) is var index and >= 0)
            {
                points[index] = point;
            }
        }

        return points;
    }

    private static int GetMinutes(TimeUnit unit, int count)
        => unit switch
        {
            TimeUnit.Minute => count,
            TimeUnit.Hour => count * 60,
            TimeUnit.Day => count * 1440,
            TimeUnit.Month => count * 43830,
            _ => count * 525960,
        };

    // Whether a local time starts a unit and stands on a step of it, counted on the zone's calendar.
    private static bool IsOnStep(DateTime localTime, TimeUnit unit, int count)
        => unit switch
        {
            TimeUnit.Minute => localTime.Second == 0 && ((localTime.Hour * 60) + localTime.Minute) % count == 0,
            TimeUnit.Hour => localTime.Minute == 0 && localTime.Second == 0 && localTime.Hour % count == 0,
            TimeUnit.Day => localTime.TimeOfDay == TimeSpan.Zero && DateOnly.FromDateTime(localTime).DayNumber % count == 0,
            TimeUnit.Month => localTime.TimeOfDay == TimeSpan.Zero && localTime.Day == 1 && ((localTime.Year * 12) + localTime.Month - 1) % count == 0,
            _ => localTime.TimeOfDay == TimeSpan.Zero && localTime.DayOfYear == 1 && localTime.Year % count == 0,
        };

    // The local times from the start of the unit holding the first to the last, a unit apart.
    private static IEnumerable<DateTime> EnumerateUnits(DateTime first, DateTime last, TimeUnit unit)
    {
        var time = unit switch
        {
            TimeUnit.Minute => new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute, 0, DateTimeKind.Unspecified),
            TimeUnit.Hour => new DateTime(first.Year, first.Month, first.Day, first.Hour, 0, 0, DateTimeKind.Unspecified),
            TimeUnit.Day => first.Date,
            TimeUnit.Month => new DateTime(first.Year, first.Month, 1, 0, 0, 0, DateTimeKind.Unspecified),
            _ => new DateTime(first.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
        };

        for (; time <= last; time = unit switch
        {
            TimeUnit.Minute => time.AddMinutes(1),
            TimeUnit.Hour => time.AddHours(1),
            TimeUnit.Day => time.AddDays(1),
            TimeUnit.Month => time.AddMonths(1),
            _ => time.AddYears(1),
        })
        {
            yield return time;
        }
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

    // A span's moment is written as a date where every moment falls on midnight of the zone's clock, and with its time otherwise.
    private string FormatPeriod(DateTime localStart)
        => Timeline.Range switch
        {
            ChartRange.Hour or ChartRange.Day => localStart.ToString("g", _culture),
            ChartRange.ThirtyDays => localStart.ToString("d", _culture),
            ChartRange.ThirteenMonths => localStart.ToString(_culture.DateTimeFormat.YearMonthPattern, _culture),
            _ => localStart.ToString(Timeline.LocalStarts.All(start => start.TimeOfDay == TimeSpan.Zero) ? "d" : "g", _culture),
        };

    // A label stands at the middle of the period it names. The labels of tier 0 always show and those of tiers 1 and 2 as the chart
    // widens; minutes and hours are labelled on round marks of the zone's clock, days and months counting back from the last.
    private List<ChartTimeLabel> CreatePeriodLabels()
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
                labels.Add(new ChartTimeLabel(GetCenter(index), FormatPeriodLabel(localStart), labelTier));
            }
        }

        return labels;
    }

    private string FormatPeriodLabel(DateTime localStart)
        => Timeline.Range switch
        {
            ChartRange.Hour or ChartRange.Day => localStart.ToString("t", _culture),
            ChartRange.ThirtyDays => FormatShortDay(localStart),
            _ => localStart.ToString("MMM", _culture),
        };

    // A span is labelled on round steps of the zone's calendar: the finest step that lays at most four labels across it always shows,
    // and finer steps nested in it, laying at most eight and twelve, show as the chart widens, each label written in the unit of the
    // coarsest step it falls on. A span of one moment labels that moment.
    private List<ChartTimeLabel> CreateSpanLabels()
    {
        var localStart = TimeZoneInfo.ConvertTime(Timeline.AxisStart, Timeline.TimeZone).DateTime;
        var localEnd = TimeZoneInfo.ConvertTime(Timeline.AxisEnd, Timeline.TimeZone).DateTime;
        var minutes = (Timeline.AxisEnd - Timeline.AxisStart).TotalMinutes;

        if (minutes <= 0)
        {
            return Timeline.Count > 0 ? [new ChartTimeLabel(0.5, PeriodTexts[0], 0)] : [];
        }

        var coarse = SpanSteps.FirstOrDefault(step => minutes / GetMinutes(step.Unit, step.Count) <= 4, SpanSteps[^1]);
        var steps = new List<(TimeUnit Unit, int Count)> { coarse };

        foreach (var reach in (int[])[8, 12])
        {
            if (SpanSteps.FirstOrDefault(step => IsNested(step, steps[^1]) && minutes / GetMinutes(step.Unit, step.Count) <= reach) is { Count: > 0 } finer)
            {
                steps.Add(finer);
            }
        }

        var (finestUnit, finestCount) = steps[^1];
        var labels = new List<ChartTimeLabel>();

        foreach (var time in EnumerateUnits(localStart, localEnd, finestUnit).Where(time => time >= localStart && IsOnStep(time, finestUnit, finestCount)))
        {
            var tier = steps.FindIndex(step => IsOnStep(time, step.Unit, step.Count));
            var position = Timeline.GetPosition(ChartTimeline.ToInstant(time, Timeline.TimeZone));
            labels.Add(new ChartTimeLabel(position, FormatSpanLabel(time, steps[tier].Unit, minutes), tier));
        }

        return labels;
    }

    // Whether every step of the coarser falls on a step of the finer, so a label shows wherever a coarser one does: a smaller count
    // of the same unit dividing it, or months dividing a year, hours a day or minutes an hour.
    private static bool IsNested((TimeUnit Unit, int Count) finer, (TimeUnit Unit, int Count) coarser)
        => finer.Unit == coarser.Unit
            ? finer.Count < coarser.Count && coarser.Count % finer.Count == 0
            : (finer.Unit, coarser.Unit) switch
            {
                (TimeUnit.Month, TimeUnit.Year) => 12 % finer.Count == 0,
                (TimeUnit.Hour, TimeUnit.Day) => 24 % finer.Count == 0,
                (TimeUnit.Minute, TimeUnit.Hour) => 60 % finer.Count == 0,
                _ => false,
            };

    private string FormatSpanLabel(DateTime localTime, TimeUnit unit, double minutes)
        => unit switch
        {
            TimeUnit.Minute or TimeUnit.Hour => localTime.ToString("t", _culture),
            TimeUnit.Day => FormatShortDay(localTime),
            TimeUnit.Month when minutes > GetMinutes(TimeUnit.Year, 1) => localTime.ToString(_culture.DateTimeFormat.YearMonthPattern.Replace("MMMM", "MMM", StringComparison.Ordinal), _culture),
            TimeUnit.Month => localTime.ToString("MMM", _culture),
            _ => localTime.ToString("yyyy", _culture),
        };

    private string FormatShortDay(DateTime localTime)
        => localTime.ToString(_culture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM", StringComparison.Ordinal), _culture);
}
