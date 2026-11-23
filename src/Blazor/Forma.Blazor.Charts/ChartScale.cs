namespace Forma.Blazor.Components.Charts;

/// <summary>A chart's one value scale, from zero or the lowest value beneath it to the highest value above it, rounded out to even steps.</summary>
internal sealed class ChartScale
{
    private ChartScale(double minimum, double maximum, double step)
    {
        Minimum = minimum;
        Maximum = maximum;
        Step = step;
        Ticks = [.. Enumerable.Range(0, (int)Math.Round((maximum - minimum) / step) + 1).Select(index => Math.Round(minimum + (index * step), 10))];
    }

    /// <summary>The scale's lowest value, at the plot's baseline edge.</summary>
    public double Minimum { get; }

    /// <summary>The scale's highest value, at the plot's far edge.</summary>
    public double Maximum { get; }

    /// <summary>The value between two gridlines.</summary>
    public double Step { get; }

    /// <summary>The values a gridline stands at, from the lowest up.</summary>
    public IReadOnlyList<double> Ticks { get; }

    /// <summary>Creates the scale reaching <paramref name="lowest"/> and <paramref name="highest"/>, and zero where <paramref name="includesZero"/>, in three or four even steps.</summary>
    /// <remarks>A step is one, two, two and a half or five times a power of ten, the smallest of those at least a quarter of the span. A scale of one value alone reaches a tenth of it either side, and one of zero alone reaches 1.</remarks>
    public static ChartScale Create(double lowest, double highest, bool includesZero = true)
    {
        var minimum = includesZero ? Math.Min(0, lowest) : lowest;
        var maximum = includesZero ? Math.Max(0, highest) : highest;

        if (maximum <= minimum)
        {
            var reach = minimum == 0 ? 1 : Math.Abs(minimum) / 10;
            (minimum, maximum) = includesZero && minimum == 0 ? (0, 1) : (minimum - reach, maximum + reach);
        }

        var step = GetStep((maximum - minimum) / 4);

        return new ChartScale(Math.Floor(Math.Round(minimum / step, 10)) * step, Math.Ceiling(Math.Round(maximum / step, 10)) * step, step);
    }

    /// <summary>Where <paramref name="value"/> stands between the scale's lowest and highest values, from 0 to 1.</summary>
    public double GetFraction(double value)
        => (value - Minimum) / (Maximum - Minimum);

    private static double GetStep(double roughStep)
    {
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
        var fraction = roughStep / magnitude;
        var multiple = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10;

        return multiple * magnitude;
    }
}
