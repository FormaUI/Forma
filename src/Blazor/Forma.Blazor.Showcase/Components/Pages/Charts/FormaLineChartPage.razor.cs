namespace Forma.Blazor.Showcase.Components.Pages.Charts;

using Forma.Blazor.Components.Charts;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaLineChartPage
{
    private IReadOnlyList<ChartSeries> _pageLoad = [];
    private IReadOnlyList<ChartSeries> _readers = [];
    private IReadOnlyList<ChartSeries> _serverUse = [];
    private IReadOnlyList<ChartSeries> _regions = [];
    private IReadOnlyList<ChartSeries> _backups = [];
    private IReadOnlyList<ChartSeries> _investment = [];
    private IReadOnlyList<ChartSeries> _returns = [];
    private IReadOnlyList<ChartSeries> _empty = [];
    private ChartThreshold? _loadTarget;
    private ChartThreshold? _useLimit;

    [Inject] private TimeProvider Clock { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
        => BuildSeries();

    /// <inheritdoc />
    protected override Task OnCultureChangedAsync()
    {
        BuildSeries();
        return Task.CompletedTask;
    }

    // Each series measured back from now, a period at a time, its values drawn from a wave so every render draws the same chart.
    private void BuildSeries()
    {
        var now = Clock.GetUtcNow();

        _pageLoad =
        [
            new(L["Median"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => index is 9 or 10 ? null : 420 + (160 * Math.Sin(index / 3.0)))),
            new(L["95th Percentile"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => index is 9 or 10 ? null : 1300 + (450 * Math.Sin(index / 3.0)) + (index % 5 * 60))),
        ];
        _loadTarget = new ChartThreshold(2000, L["Target"]);
        _readers = [new(L["Readers"], ShowcaseCharts.CreatePoints(now, 60, TimeSpan.FromMinutes(1), index => 30 + (12 * Math.Sin(index / 6.0)) + (index % 7)))];
        _serverUse =
        [
            new(L["CPU"], ShowcaseCharts.CreateDailyPoints(now, 30, index => 35 + (20 * Math.Sin(index / 4.0)) + (index % 3 * 4))),
            new(L["Memory"], ShowcaseCharts.CreateDailyPoints(now, 30, index => 58 + (index * 0.6))),
        ];
        _useLimit = new ChartThreshold(80, L["Limit"]);
        _regions =
        [
            new(L["Europe"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => 180 + (40 * Math.Sin(index / 4.0)))),
            new(L["Americas"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => 260 + (50 * Math.Cos(index / 5.0)))),
            new(L["Asia"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => 340 + (30 * Math.Sin(index / 3.0)))),
            new(L["Oceania"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => 420 + (60 * Math.Cos(index / 4.0)))),
        ];
        _backups = [new(L["Backups"], ShowcaseCharts.CreateDailyPoints(now, 30, index => index is 4 or 12 or 13 or 14 or 21 or 26 or 27 ? 3 + (index % 4) : null))];
        _empty = [new(L["Median"], [])];
        _investment = [CreateInvestment(now)];
        _returns =
        [
            new(L["This Account"], ShowcaseCharts.CreateMonthlyPoints(now, 30, index => (index * 0.004) + (0.02 * Math.Sin(index / 3.0)), 4)),
            new(L["Market Index"], ShowcaseCharts.CreateMonthlyPoints(now, 30, index => (index * 0.003) + (0.03 * Math.Cos(index / 4.0)) - 0.02, 4)),
        ];
    }

    // An account's recorded value at each entry over two and a half years, each deposit, withdrawal and fee marked.
    private ChartSeries CreateInvestment(DateTimeOffset now)
    {
        var deposit = new ChartMark(L["Deposit"]);
        var withdrawal = new ChartMark(L["Withdrawal"]);
        var fee = new ChartMark(L["Fee"]);
        var today = TimeZoneInfo.ConvertTime(now, Clock.LocalTimeZone).Date;
        DateTimeOffset GetDay(int months)
        {
            return ShowcaseCharts.AtMidnight(today.AddMonths(months - 30), Clock.LocalTimeZone);
        }

        return new(L["Recorded Value"],
        [
            new(GetDay(0), 10_000) { Mark = deposit, Note = L["The account opened"] },
            new(GetDay(3), 10_240),
            new(GetDay(7), 10_610),
            new(GetDay(8), 12_610) { Mark = deposit },
            new(GetDay(12), 12_530) { Mark = fee, Note = L["Yearly management fee"] },
            new(GetDay(16), 13_080),
            new(GetDay(19), 11_580) { Mark = withdrawal },
            new(GetDay(24), 11_490) { Mark = fee, Note = L["Yearly management fee"] },
            new(GetDay(27), 12_220),
            new(GetDay(30), 12_760),
        ])
        { IsFilled = true };
    }
}
