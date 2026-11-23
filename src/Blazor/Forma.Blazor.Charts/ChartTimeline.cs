namespace Forma.Blazor.Components.Charts;

/// <summary>
/// The slots a chart's time axis holds, each a period or a moment: the periods a fixed range divides into, ending with the period
/// holding the chart's end, or the moments a span's points stand at, each slot placed along the axis and holding a stretch of it a
/// reader points at.
/// </summary>
internal sealed class ChartTimeline
{
    private readonly DateTimeOffset[] _starts;
    private readonly DateTimeOffset _end;
    private readonly double[] _centers;
    private readonly (double Start, double End)[] _bounds;

    private ChartTimeline(ChartRange range, DateTimeOffset[] starts, DateTimeOffset end, double[] centers, (double Start, double End)[] bounds, DateTimeOffset axisStart, TimeZoneInfo timeZone)
    {
        Range = range;
        _starts = starts;
        _end = end;
        _centers = centers;
        _bounds = bounds;
        AxisStart = axisStart;
        TimeZone = timeZone;
        LocalStarts = [.. starts.Select(start => TimeZoneInfo.ConvertTime(start, timeZone).DateTime)];
    }

    /// <summary>The range the timeline divides.</summary>
    public ChartRange Range { get; }

    /// <summary>The time zone the timeline's clock reads.</summary>
    public TimeZoneInfo TimeZone { get; }

    /// <summary>How many slots the timeline holds.</summary>
    public int Count
        => _starts.Length;

    /// <summary>Each slot's start, a period's or a moment's, in order.</summary>
    public IReadOnlyList<DateTimeOffset> Starts
        => _starts;

    /// <summary>Each slot's start on the time zone's clock, in order.</summary>
    public IReadOnlyList<DateTime> LocalStarts { get; }

    /// <summary>The time the axis starts at, its inline start.</summary>
    public DateTimeOffset AxisStart { get; }

    /// <summary>The time the axis ends at, its inline end.</summary>
    public DateTimeOffset AxisEnd
        => _end;

    /// <summary>Creates the timeline of <paramref name="range"/> ending with the period that holds <paramref name="end"/>, on the clock of <paramref name="timeZone"/>.</summary>
    /// <remarks>
    /// A minute or an hour is counted back from the last one's start on the universal clock, so a day crossing a change of the zone's
    /// offset holds the hours that really passed, labelled as the zone's clock read them. A day or a month starts at the zone's
    /// midnight, or at the first moment after it where the zone's clock skips midnight.
    /// </remarks>
    public static ChartTimeline Create(ChartRange range, DateTimeOffset end, TimeZoneInfo timeZone)
    {
        var localEnd = TimeZoneInfo.ConvertTime(end, timeZone).DateTime;

        switch (range)
        {
            case ChartRange.Hour:
                return CreateEven(range, end.AddTicks(-(localEnd.Ticks % TimeSpan.TicksPerMinute)), TimeSpan.FromMinutes(1), 60, timeZone);
            case ChartRange.Day:
                return CreateEven(range, end.AddTicks(-(localEnd.Ticks % TimeSpan.TicksPerHour)), TimeSpan.FromHours(1), 24, timeZone);
            case ChartRange.ThirtyDays:
                {
                    var lastDay = localEnd.Date;
                    var starts = Enumerable.Range(0, 31).Select(index => ToInstant(lastDay.AddDays(index - 29), timeZone)).ToArray();

                    return CreatePeriods(range, starts[..30], starts[30], timeZone);
                }

            case ChartRange.ThirteenMonths:
                {
                    var lastMonth = new DateTime(localEnd.Year, localEnd.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                    var starts = Enumerable.Range(0, 14).Select(index => ToInstant(lastMonth.AddMonths(index - 12), timeZone)).ToArray();

                    return CreatePeriods(range, starts[..13], starts[13], timeZone);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(range), range, "A fixed range is an hour, a day, thirty days or thirteen months.");
        }
    }

    /// <summary>Creates the timeline of a span from <paramref name="start"/> to <paramref name="end"/>, its slots the moments of <paramref name="times"/> within it, each placed by its time.</summary>
    /// <remarks>A slot holds the stretch of the axis nearer its moment than either neighbor's. A span whose start and end are one moment places it at the middle.</remarks>
    public static ChartTimeline CreateSpan(IEnumerable<DateTimeOffset> times, DateTimeOffset start, DateTimeOffset end, TimeZoneInfo timeZone)
    {
        var moments = times.Where(time => time >= start && time <= end).Distinct().Order().ToArray();
        var length = (end - start).Ticks;
        var centers = moments.Select(moment => length == 0 ? 0.5 : (double)(moment - start).Ticks / length).ToArray();
        var bounds = centers.Select((center, index) => (
            index == 0 ? 0 : (centers[index - 1] + center) / 2,
            index == centers.Length - 1 ? 1 : (center + centers[index + 1]) / 2)).ToArray();

        return new ChartTimeline(ChartRange.Span, moments, end, centers, bounds, start, timeZone);
    }

    /// <summary>Where the slot at <paramref name="index"/> stands along the axis, from 0 at its inline start to 1 at its end.</summary>
    public double GetCenter(int index)
        => _centers[index];

    /// <summary>The stretch of the axis the slot at <paramref name="index"/> holds, from 0 at its inline start to 1 at its end.</summary>
    public (double Start, double End) GetBounds(int index)
        => _bounds[index];

    /// <summary>Where <paramref name="time"/> stands along the axis, from 0 at its inline start to 1 at its end.</summary>
    public double GetPosition(DateTimeOffset time)
    {
        var length = (AxisEnd - AxisStart).Ticks;

        return length == 0 ? 0.5 : (double)(time - AxisStart).Ticks / length;
    }

    /// <summary>The index of the slot holding <paramref name="time"/>, the period holding it or the moment it is, or -1 for a time outside the timeline.</summary>
    public int FindIndex(DateTimeOffset time)
    {
        if (Range is ChartRange.Span)
        {
            return Array.BinarySearch(_starts, time) is var moment and >= 0 ? moment : -1;
        }

        if (time < _starts[0] || time >= _end)
        {
            return -1;
        }

        var index = Array.BinarySearch(_starts, time);

        return index >= 0 ? index : ~index - 1;
    }

    private static ChartTimeline CreateEven(ChartRange range, DateTimeOffset lastStart, TimeSpan length, int count, TimeZoneInfo timeZone)
        => CreatePeriods(range, [.. Enumerable.Range(0, count).Select(index => lastStart - (length * (count - 1 - index)))], lastStart + length, timeZone);

    // Periods share the axis evenly, each centered on its own stretch.
    private static ChartTimeline CreatePeriods(ChartRange range, DateTimeOffset[] starts, DateTimeOffset end, TimeZoneInfo timeZone)
    {
        var count = starts.Length;
        var centers = Enumerable.Range(0, count).Select(index => (index + 0.5) / count).ToArray();
        var bounds = Enumerable.Range(0, count).Select(index => ((double)index / count, (double)(index + 1) / count)).ToArray();

        return new ChartTimeline(range, starts, end, centers, bounds, starts[0], timeZone);
    }

    /// <summary>The moment the clock of <paramref name="timeZone"/> reads <paramref name="localTime"/>, the first where it reads it twice, and the first moment after it where the clock skips it.</summary>
    public static DateTimeOffset ToInstant(DateTime localTime, TimeZoneInfo timeZone)
    {
        while (timeZone.IsInvalidTime(localTime))
        {
            localTime = localTime.AddMinutes(30);
        }

        var offset = timeZone.IsAmbiguousTime(localTime) ? timeZone.GetAmbiguousTimeOffsets(localTime).Max() : timeZone.GetUtcOffset(localTime);

        return new DateTimeOffset(localTime, offset);
    }
}
