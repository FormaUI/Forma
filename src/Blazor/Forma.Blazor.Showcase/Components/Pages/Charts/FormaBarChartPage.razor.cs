namespace Forma.Blazor.Showcase.Components.Pages.Charts;

using Forma.Blazor.Components.Charts;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaBarChartPage
{
    private IReadOnlyList<ChartSeries> _failures = [];
    private IReadOnlyList<ChartSeries> _signups = [];
    private IReadOnlyList<ChartSeries> _storage = [];
    private IReadOnlyList<ChartSeries> _regions = [];
    private IReadOnlyList<ChartSeries> _change = [];
    private IReadOnlyList<ChartSeries> _empty = [];
    private ChartThreshold? _alertLevel;

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

    private void BuildSeries()
    {
        var now = Clock.GetUtcNow();

        _failures =
        [
            new(L["Failed Uploads"], ShowcaseCharts.CreateDailyPoints(now, 30, index => index % 9 == 4 ? null : 6 + (index % 5 * 3) + (index % 7 == 0 ? 12 : 0))),
            new(L["Failed Payments"], ShowcaseCharts.CreateDailyPoints(now, 30, index => index % 9 == 4 ? null : 2 + (index % 4 * 2))),
        ];
        _alertLevel = new ChartThreshold(20, L["Alert Level"]);
        _signups = [new(L["Sign-ups"], ShowcaseCharts.CreateDailyPoints(now, 7, index => 40 + (index * 6) + (index % 3 * 9)))];
        _storage = [new(L["Storage Used"], ShowcaseCharts.CreateMonthlyPoints(now, 13, index => 420 + (index * 38) + (index % 3 * 12)))];
        _regions =
        [
            new(L["Europe"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => 120 + (60 * Math.Sin(index / 4.0)))),
            new(L["Americas"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => 150 + (70 * Math.Cos(index / 4.0)))),
            new(L["Asia"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => 90 + (40 * Math.Sin(index / 3.0)))),
            new(L["Oceania"], ShowcaseCharts.CreatePoints(now, 24, TimeSpan.FromHours(1), index => 40 + (20 * Math.Cos(index / 5.0)))),
        ];
        _change = [new(L["Storage Change"], ShowcaseCharts.CreateMonthlyPoints(now, 13, index => (index % 4 == 3 ? -18 : 24) + (index % 3 * 7)))];
        _empty = [new(L["Failed Uploads"], [])];
    }
}
