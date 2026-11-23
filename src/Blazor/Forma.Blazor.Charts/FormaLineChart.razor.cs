namespace Forma.Blazor.Components.Charts;

using Microsoft.AspNetCore.Components;

/// <summary>Renders one to four named series over a shared time axis as lines, broken where a period has no data, with an optional threshold line and a band between two series.</summary>
public sealed partial class FormaLineChart : FormaElementBase
{
    /// <summary>The chart's title, which names the figure and its table.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The chart's series, one to four, each drawn in the color, dash and marker of its place among them.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<ChartSeries> Series { get; set; } = default!;

    /// <summary>The span of time the chart covers.</summary>
    [Parameter] public ChartRange Range { get; set; } = ChartRange.Day;

    /// <summary>The time a span starts at, where null is its earliest point's time.</summary>
    [Parameter] public DateTimeOffset? Start { get; set; }

    /// <summary>The time the chart's last period holds, where null is now, or the time a span ends at, where null is its latest point's time.</summary>
    [Parameter] public DateTimeOffset? End { get; set; }

    /// <summary>The .NET numeric format the chart writes its values in, where null writes up to two decimals.</summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>The value the chart marks with a labelled line.</summary>
    [Parameter] public ChartThreshold? Threshold { get; set; }

    /// <summary>The band the chart fills between two of its series.</summary>
    [Parameter] public ChartBand? Band { get; set; }

    /// <summary>Whether the chart's value scale reaches zero; false fits the scale to the values.</summary>
    [Parameter] public bool IncludeZero { get; set; } = true;

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
        ChartValidation.ValidateSpan(Range, Start, End);

        if (Band is { } band)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(band.LowerSeriesIndex, nameof(Band));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(band.LowerSeriesIndex, Series.Count, nameof(Band));
            ArgumentOutOfRangeException.ThrowIfNegative(band.UpperSeriesIndex, nameof(Band));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(band.UpperSeriesIndex, Series.Count, nameof(Band));

            if (band.LowerSeriesIndex == band.UpperSeriesIndex)
            {
                throw new ArgumentException("A band lies between two different series.", nameof(Band));
            }
        }
    }
}
