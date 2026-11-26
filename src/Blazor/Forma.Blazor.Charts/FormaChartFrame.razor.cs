namespace Forma.Blazor.Components.Charts;

using System.Globalization;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>Renders a chart of figures over time: its caption, legend, plot, readouts, time axis and table, its series drawn as lines or bars.</summary>
/// <remarks>
/// The plot is laid out in fractions of its size: the lines and the band in a drawing stretched over it and mirrored as a whole in a
/// right-to-left culture, everything else in elements placed by logical properties. Each period is a column over the plot holding its
/// readout, shown as the pointer hovers it or, from the keyboard, as a native range control reaches it.
/// </remarks>
public sealed partial class FormaChartFrame : FormaElementBase
{
    private const string DefaultValueFormat = "#,0.##";

    // How near either end of the time axis a label's period stands for the label to align with that end at any width, a sixteenth.
    private const double EdgeLabelReach = 0.0625;

    /// <summary>The marks the chart draws its series with.</summary>
    [Parameter] public ChartKind Kind { get; set; }

    /// <summary>The chart's title, which names the figure and its table.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The chart's series.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<ChartSeries> Series { get; set; } = default!;

    /// <summary>The span of time the chart covers.</summary>
    [Parameter] public ChartRange Range { get; set; }

    /// <summary>The time a span starts at.</summary>
    [Parameter] public DateTimeOffset? Start { get; set; }

    /// <summary>The time the chart's last period holds, or a span ends at.</summary>
    [Parameter] public DateTimeOffset? End { get; set; }

    /// <summary>The .NET numeric format the chart writes its values in.</summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>The value the chart marks with a labelled line.</summary>
    [Parameter] public ChartThreshold? Threshold { get; set; }

    /// <summary>The band the chart fills between two of its series.</summary>
    [Parameter] public ChartBand? Band { get; set; }

    /// <summary>Whether the chart's value scale reaches zero.</summary>
    [Parameter] public bool IncludeZero { get; set; } = true;

    /// <summary>Whether a toggle beneath the chart shows its table to every reader.</summary>
    [Parameter] public bool ShowTable { get; set; }

    [Inject] private TimeProvider Clock { get; set; } = default!;

    private ChartLayout? _layout;
    private CultureInfo? _layoutCulture;
    private bool _isLayoutStale = true;
    private int _readIndex = -1;

    private string TitleId
        => $"{ElementId}-title";

    private ChartLayout Layout
    {
        get
        {
            var culture = Context.CultureState.Culture;

            if (_layout is null || _isLayoutStale || !ReferenceEquals(culture, _layoutCulture))
            {
                _layout = ChartLayout.Create(Series, Range, Start, End, Clock.GetUtcNow(), Clock.LocalTimeZone, culture, ValueFormat ?? DefaultValueFormat, Threshold, IncludeZero);
                _layoutCulture = culture;
                _isLayoutStale = false;
            }

            return _layout;
        }
    }

    private string TableViewClass
        => ShowTable ? "forma-chart-table-view is-shown" : "forma-chart-table-view";

    // Whether the legend draws: for two or more series, or for the marks a line chart's points carry.
    private bool HasLegend
        => Series.Count > 1 || HasMarkLegend;

    // Whether the legend keys the marks, which a line chart draws its points in.
    private bool HasMarkLegend
        => Kind is ChartKind.Line && Layout.Marks.Count > 0;

    // The period the keyboard reads, the last until the reader moves.
    private int ReadIndex
        => _readIndex >= 0 && _readIndex < Layout.Timeline.Count ? _readIndex : Layout.Timeline.Count - 1;

    // The gutter at the plot's inline start that the value labels stand in, as wide as the longest of them with room around it,
    // which the drawing and the time axis stand clear of.
    private string ValueGutterStyle
        => new CssStyleBuilder()
            .Add("--forma-chart-value-gutter", CssLength.FromCh(Layout.Scale.Ticks.Select(tick => Layout.FormatValue(tick).Length).DefaultIfEmpty(0).Max() + 2))
            .ToString();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _isLayoutStale = true;
    }

    private static string GetSeriesClass(int seriesIndex)
        => $"forma-chart-series-{seriesIndex + 1}";

    private static string FormatPercent(double fraction)
        => (fraction * 100).ToString("0.###", CultureInfo.InvariantCulture) + "%";

    // A ratio a stylesheet reads, as a plain number.
    private static string FormatRatio(double ratio)
        => ratio.ToString("0.####", CultureInfo.InvariantCulture);

    private static string FormatCoordinate(double fraction)
        => (fraction * 1000).ToString("0.##", CultureInfo.InvariantCulture);

    private void SetReadIndex(int index)
        => _readIndex = index;

    // The points of a run of a series' values, as a polyline's points along the drawing's thousand units each way.
    private string GetLinePoints(int seriesIndex, int first, int last)
        => string.Join(' ', Enumerable.Range(first, last - first + 1).Select(index => GetPoint(index, Layout.Values[seriesIndex][index]!.Value)));

    // The outline of a run of the band, along its upper series and back along its lower one.
    private string GetBandPoints(ChartBand band, int first, int last)
    {
        var upper = Enumerable.Range(first, last - first + 1).Select(index => GetPoint(index, Layout.Values[band.UpperSeriesIndex][index]!.Value));
        var lower = Enumerable.Range(first, last - first + 1).Reverse().Select(index => GetPoint(index, Layout.Values[band.LowerSeriesIndex][index]!.Value));

        return string.Join(' ', upper.Concat(lower));
    }

    // The area between a run of a series' line and the baseline.
    private string GetFillPoints(int seriesIndex, int first, int last)
    {
        var baseline = FormatCoordinate(1 - Layout.GetBaseline());

        return $"{GetLinePoints(seriesIndex, first, last)} {FormatCoordinate(Layout.GetCenter(last))},{baseline} {FormatCoordinate(Layout.GetCenter(first))},{baseline}";
    }

    private string GetPoint(int index, double value)
        => $"{FormatCoordinate(Layout.GetCenter(index))},{FormatCoordinate(1 - Layout.Scale.GetFraction(value))}";

    // The bar's extent between the baseline and its value, from the plot's bottom and from its top.
    private string GetBarStyle(double value)
    {
        var baseline = Layout.GetBaseline();
        var reach = Layout.Scale.GetFraction(value);

        return $"inset-block-end: {FormatPercent(Math.Min(baseline, reach))}; inset-block-start: {FormatPercent(1 - Math.Max(baseline, reach))};";
    }

    private string GetValueText(int seriesIndex, int index)
        => Layout.Values[seriesIndex][index] is { } value ? Layout.FormatValue(value) : L["No data"];

    // What the mark and the note of a series' point in a slot say beside its value.
    private string? GetPointNote(int seriesIndex, int index)
        => Layout.Points[seriesIndex][index] is { } point && (point.Mark is not null || !string.IsNullOrWhiteSpace(point.Note))
            ? string.Join(": ", new[] { point.Mark?.Name, point.Note }.Where(text => !string.IsNullOrWhiteSpace(text)))
            : null;

    // What the keyboard's range control says of a period: the period, then each series' name, value, and what its point notes.
    private string GetReading(int index)
        => string.Join(", ", Series.Select((series, seriesIndex) => GetPointNote(seriesIndex, index) is { } note ? $"{series.Name} {GetValueText(seriesIndex, index)} ({note})" : $"{series.Name} {GetValueText(seriesIndex, index)}").Prepend(Layout.PeriodTexts[index]));

    private static string GetMarkClass(int markIndex)
        => $"forma-chart-mark-{markIndex + 1}";

    private static string GetGridlineClass(double tick)
        => tick == 0 ? "forma-chart-gridline is-baseline" : "forma-chart-gridline";

    private static string GetBarClass(int seriesIndex, double value)
        => value < 0 ? $"forma-chart-bar {GetSeriesClass(seriesIndex)} is-negative" : $"forma-chart-bar {GetSeriesClass(seriesIndex)}";

    // How far up the plot a value stands.
    private string GetHeightStyle(double value)
        => $"inset-block-end: {FormatPercent(Layout.Scale.GetFraction(value))};";

    private string GetPeriodClass(int index)
        => index == ReadIndex ? "forma-chart-period is-read" : "forma-chart-period";

    // A slot's column spans the stretch of the axis nearer it than its neighbors, and the slot stands at its own place within it.
    private string GetPeriodStyle(int index)
    {
        var (start, end) = Layout.Timeline.GetBounds(index);
        var center = end > start ? (Layout.GetCenter(index) - start) / (end - start) : 0.5;

        var size = end - start;

        return $"inset-inline-start: {FormatPercent(start)}; inline-size: {FormatPercent(size)}; --forma-chart-slot-center: {FormatPercent(center)}; --forma-chart-slot-offset: {FormatRatio(start / size)}; --forma-chart-slot-span: {FormatRatio(1 / size)};";
    }

    private string GetMarkerClass(int seriesIndex, int index)
    {
        var markerClass = Layout.IsStanding(seriesIndex, index) ? $"forma-chart-marker {GetSeriesClass(seriesIndex)} is-standing" : $"forma-chart-marker {GetSeriesClass(seriesIndex)}";

        return Layout.Points[seriesIndex][index]?.Mark is { } mark ? $"{markerClass} is-marked {GetMarkClass(Layout.GetMarkIndex(mark))}" : markerClass;
    }

    // A readout opens away from the nearer edge, so it stays within the plot.
    private string GetReadoutClass(int index)
        => Layout.GetCenter(index) > 0.5 ? "forma-chart-readout is-end" : "forma-chart-readout";

    // A label near either end aligns with that end rather than standing centered past it: within the nearest sixteenth of the
    // axis at any width, and within the nearest eighth on a narrow chart.
    private static string GetTimeLabelClass(ChartTimeLabel label)
    {
        var center = label.Position;
        var anchor = center switch
        {
            < EdgeLabelReach => " is-start is-near-start",
            < 2 * EdgeLabelReach => " is-near-start",
            > 1 - EdgeLabelReach => " is-end is-near-end",
            > 1 - (2 * EdgeLabelReach) => " is-near-end",
            _ => string.Empty,
        };

        return $"forma-chart-time-label is-tier-{label.Tier}{anchor}";
    }

    private static string GetTimeLabelStyle(ChartTimeLabel label)
        => $"--forma-chart-label-center: {FormatPercent(label.Position)};";

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add("forma-chart").Add(Kind is ChartKind.Line ? "forma-line-chart" : "forma-bar-chart");
    }
}
