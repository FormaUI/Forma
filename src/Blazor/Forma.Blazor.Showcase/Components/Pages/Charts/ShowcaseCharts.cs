namespace Forma.Blazor.Showcase.Components.Pages.Charts;

using Forma.Blazor.Components.Charts;

/// <summary>The made-up measurements the charts' pages draw.</summary>
internal static class ShowcaseCharts
{
    /// <summary>Creates <paramref name="count"/> points, one each <paramref name="interval"/> back from <paramref name="now"/>, the first the oldest, each valued by its index.</summary>
    public static IReadOnlyList<ChartPoint> CreatePoints(DateTimeOffset now, int count, TimeSpan interval, Func<int, double?> getValue)
        => [.. Enumerable.Range(0, count).Select(index => new ChartPoint(now - (interval * (count - 1 - index)), Round(getValue(index))))];

    /// <summary>Creates a point a day back from <paramref name="now"/> for each of <paramref name="count"/> days, the first the oldest.</summary>
    public static IReadOnlyList<ChartPoint> CreateDailyPoints(DateTimeOffset now, int count, Func<int, double?> getValue)
        => [.. Enumerable.Range(0, count).Select(index => new ChartPoint(now.AddDays(index - (count - 1)), Round(getValue(index))))];

    /// <summary>Creates a point a month back from <paramref name="now"/> for each of <paramref name="count"/> months, the first the oldest, each rounded to <paramref name="decimals"/> places.</summary>
    public static IReadOnlyList<ChartPoint> CreateMonthlyPoints(DateTimeOffset now, int count, Func<int, double?> getValue, int decimals = 0)
        => [.. Enumerable.Range(0, count).Select(index => new ChartPoint(now.AddMonths(index - (count - 1)), Round(getValue(index), decimals)))];

    /// <summary>The moment midnight of <paramref name="localDate"/> begins on the clock of <paramref name="timeZone"/>.</summary>
    public static DateTimeOffset AtMidnight(DateTime localDate, TimeZoneInfo timeZone)
        => new(localDate.Date, timeZone.GetUtcOffset(localDate.Date));

    private static double? Round(double? value, int decimals = 0)
        => value is { } measured ? Math.Round(measured, decimals) : null;
}
