namespace Forma.Blazor.Components.Charts;

using Microsoft.AspNetCore.Components;

/// <summary>Renders one to four named series over a shared time axis as bars, each period's bars side by side and missing where a period has no data, with an optional threshold line.</summary>
public sealed partial class FormaBarChart : FormaElementBase
{
    /// <summary>The chart's title, which names the figure and its table.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The chart's series, one to four, each drawn in the color of its place among them and standing in that place in every period.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<ChartSeries> Series { get; set; } = default!;

    /// <summary>The span of time the chart covers, one of the fixed ranges.</summary>
    [Parameter] public ChartRange Range { get; set; } = ChartRange.ThirtyDays;

    /// <summary>The time the chart's last period holds, where null is now.</summary>
    [Parameter] public DateTimeOffset? End { get; set; }

    /// <summary>The .NET numeric format the chart writes its values in, where null writes up to two decimals.</summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>The value the chart marks with a labelled line.</summary>
    [Parameter] public ChartThreshold? Threshold { get; set; }

    /// <summary>Whether a toggle beneath the chart shows its table to every reader.</summary>
    [Parameter] public bool ShowTable { get; set; }

    private FormaChartFrame Frame { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Frame is null ? default : Frame.RootElement;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ChartValidation.Validate(Title, Series, Threshold);

        if (Range is ChartRange.Span)
        {
            throw new ArgumentException("A bar chart draws a bar for each period of a fixed range, and a span has no periods; draw a span with a line chart.", nameof(Range));
        }
    }
}
