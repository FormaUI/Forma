# Forma.Blazor.Charts

`FormaLineChart` and `FormaBarChart`, the charts of figures over time of the **Forma** component library.
Each draws one to four named series over the last hour, day, 7 days, 30 days or 13 months, or the line chart over any span, a gap wherever a period
has no data, with a threshold line, and on the line chart a band between two series. A reader reads a
period's values by hovering it or by moving through the periods from the keyboard, and assistive
technology reads every value as a table.

**.NET 11 · Blazor components.**

## Install

```pwsh
dotnet add package formaui-net.Blazor.Charts --prerelease
```

The package depends on `formaui-net.Blazor`, which restores beside it and carries the tokens the
charts draw from. Its components are in the `Forma.Blazor.Components.Charts` namespace. The charts need
no services of their own, so the host registers Forma as the library's README sets out and nothing more.

## A line chart

```razor
@using Forma.Blazor.Components.Charts
@inject IPageMetrics Metrics

<FormaLineChart Title="Page load"
                Series="_pageLoad"
                Range="ChartRange.Day"
                ValueFormat="#,0' ms'"
                Band="new ChartBand(0, 1)"
                Threshold="_target" />

@code {
    private IReadOnlyList<ChartSeries> _pageLoad = [];
    private readonly ChartThreshold _target = new(2000, "Target");

    protected override async Task OnInitializedAsync()
    {
        var hours = await Metrics.GetPageLoadByHourAsync();

        _pageLoad =
        [
            new("Median", [.. hours.Select(hour => new ChartPoint(hour.Start, hour.Median))]),
            new("95th percentile", [.. hours.Select(hour => new ChartPoint(hour.Start, hour.Percentile95))]),
        ];
    }
}
```

`Series` holds one to four `ChartSeries`, each a name and its `ChartPoint`s, a time and a value. A point
stands in the period that holds its time; a period with no point, or with a point whose value is null,
is a gap, which breaks a line and leaves a bar out, and never reads as zero. A value and a threshold are finite
numbers: a chart refuses `NaN` or an infinity with an `ArgumentException` naming the series and the point's time.

`FormaBarChart` takes the same parameters but `Band`, `Start` and `IncludeZero`, and draws each period's
bars side by side in the order of `Series`.

## A span and its events

A span draws a history at the times its points were recorded, such as an account's value since it opened:

```razor
@inject IAccountHistory Accounts

<FormaLineChart Title="Retirement account"
                Series="_account"
                Range="ChartRange.Span"
                ValueFormat="#,0' €'"
                IncludeZero="false"
                ShowTable="true" />

@code {
    private static readonly ChartMark Deposit = new("Deposit");
    private static readonly ChartMark Fee = new("Fee");

    private IReadOnlyList<ChartSeries> _account = [];

    protected override async Task OnInitializedAsync()
    {
        var entries = await Accounts.GetEntriesAsync();

        _account =
        [
            new("Recorded value", [.. entries.Select(entry => new ChartPoint(entry.Day, entry.Value)
            {
                Mark = entry.Kind switch { EntryKind.Deposit => Deposit, EntryKind.Fee => Fee, _ => null },
                Note = entry.Explanation,
            })]) { IsFilled = true },
        ];
    }
}
```

The span runs from `Start` to `End`, or from the first point's time to the last's where either is null, and
its axis labels round steps of the calendar, from minutes to years. A point's `Mark`, a `ChartMark`, draws it
in the mark's own shape and names it in the legend, up to four marks; its mark and its `Note` stand beside its
value in the readout and the table. `IsFilled` washes the area beneath a series' line, `IncludeZero="false"`
fits the scale to the values rather than reaching zero, and `ShowTable` puts the table behind a toggle every
reader opens.

## The parameters

| Parameter | What it sets |
| --- | --- |
| `Title` | The chart's title, which names the figure and its table |
| `Series` | One to four `ChartSeries` |
| `Range` | `ChartRange.Hour` of one-minute periods, `Day` of one-hour periods, `SevenDays` or `ThirtyDays` of one-day periods, `ThirteenMonths` of one-month periods, or on the line chart a `Span` whose points stand at their own times |
| `Start` | On the line chart, the time a span starts at, its first point's time where it is null |
| `End` | The time the last period holds, now where it is null, or the time a span ends at, its last point's time where it is null |
| `ValueFormat` | The .NET numeric format of the axis, the readout and the table, up to two decimals where it is null |
| `Threshold` | A `ChartThreshold`, a value and its label, drawn as a line across the plot |
| `Band` | On the line chart, a `ChartBand` between the series at two indexes in `Series` |
| `IncludeZero` | On the line chart, whether the scale reaches zero, true by default |
| `ShowTable` | Whether a toggle beneath the chart shows its table to every reader |

## Time and culture

The periods start on the minute, hour, day or month of the time zone the host's `TimeProvider` names as
local, so a host that registers a provider in the reader's zone draws the reader's days. The labels, the
readout and the table are written in the culture Forma's culture state holds, and a right-to-left
culture runs time from the right.

## Colors and themes

Each series takes the color of its place in `Series`, so a host keeps a series in its place to keep its
color. A line also draws its own dash and marker, and each period's bars stand in the series' order, so a
series is told apart without color; forced colors and print give each series' bars but the first's a texture.

The colors are tokens a theme sets, a theme with a dark scheme writing each as `light-dark(light, dark)`:

| Token | Light default | A dark scheme's value |
| --- | --- | --- |
| `--chart-series-1-color` | `#2a78d6` | `#3987e5` |
| `--chart-series-2-color` | `#eb6834` | `#d95926` |
| `--chart-series-3-color` | `#1baf7a` | `#199e70` |
| `--chart-series-4-color` | `#eda100` | `#c98500` |

The light defaults are validated as a set against the paper, and the dark values against a dark surface,
so adjacent series stay apart for color-blind readers. The grid, the baseline and the threshold follow the
gray scale and the ink, `--chart-grid-color`, `--chart-baseline-color` and `--chart-threshold-color`, and
`--chart-surface-color` is the backdrop a chart stands on, which rings a marker. `--chart-plot-block-size`
sets the plot's height.

The [charts design](https://github.com/FormaUI/Forma/blob/main/docs/charts-design.md) describes the
charts in full.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
