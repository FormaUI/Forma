namespace Forma.Blazor.Components.Charts;

/// <summary>The periods a chart's range divides into, ending with the period holding the chart's end, each starting on a minute, hour, day or month of the chart's time zone.</summary>
internal sealed class ChartTimeline
{
    private readonly DateTimeOffset[] _starts;
    private readonly DateTimeOffset _end;

    private ChartTimeline(ChartRange range, DateTimeOffset[] starts, DateTimeOffset end, TimeZoneInfo timeZone)
    {
        Range = range;
        _starts = starts;
        _end = end;
        LocalStarts = [.. starts.Select(start => TimeZoneInfo.ConvertTime(start, timeZone).DateTime)];
    }

    /// <summary>The range the timeline divides.</summary>
    public ChartRange Range { get; }

    /// <summary>How many periods the timeline holds.</summary>
    public int Count
        => _starts.Length;

    /// <summary>Each period's start on the time zone's clock, in order.</summary>
    public IReadOnlyList<DateTime> LocalStarts { get; }

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

                    return new ChartTimeline(range, starts[..30], starts[30], timeZone);
                }

            case ChartRange.ThirteenMonths:
                {
                    var lastMonth = new DateTime(localEnd.Year, localEnd.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                    var starts = Enumerable.Range(0, 14).Select(index => ToInstant(lastMonth.AddMonths(index - 12), timeZone)).ToArray();

                    return new ChartTimeline(range, starts[..13], starts[13], timeZone);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(range), range, "The range is an hour, a day, thirty days or thirteen months.");
        }
    }

    /// <summary>The index of the period holding <paramref name="time"/>, or -1 for a time outside the timeline.</summary>
    public int FindIndex(DateTimeOffset time)
    {
        if (time < _starts[0] || time >= _end)
        {
            return -1;
        }

        var index = Array.BinarySearch(_starts, time);

        return index >= 0 ? index : ~index - 1;
    }

    private static ChartTimeline CreateEven(ChartRange range, DateTimeOffset lastStart, TimeSpan length, int count, TimeZoneInfo timeZone)
    {
        var starts = Enumerable.Range(0, count).Select(index => lastStart - (length * (count - 1 - index))).ToArray();

        return new ChartTimeline(range, starts, lastStart + length, timeZone);
    }

    // The moment the zone's clock reads the local time, the first where it reads it twice, and the first moment after it where the
    // clock skips it.
    private static DateTimeOffset ToInstant(DateTime localTime, TimeZoneInfo timeZone)
    {
        while (timeZone.IsInvalidTime(localTime))
        {
            localTime = localTime.AddMinutes(30);
        }

        var offset = timeZone.IsAmbiguousTime(localTime) ? timeZone.GetAmbiguousTimeOffsets(localTime).Max() : timeZone.GetUtcOffset(localTime);

        return new DateTimeOffset(localTime, offset);
    }
}
