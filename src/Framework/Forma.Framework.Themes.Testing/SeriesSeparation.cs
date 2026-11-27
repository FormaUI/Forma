namespace Forma.Framework.Themes.Testing;

using System.Globalization;

/// <summary>Two adjacent chart series' tokens, held to a least distance apart in a color vision.</summary>
public sealed record SeriesSeparation
{
    /// <summary>The color scheme the series are drawn in.</summary>
    public required ColorSchemes Scheme { get; init; }

    /// <summary>The token of the series a chart takes first.</summary>
    public required string First { get; init; }

    /// <summary>The token of the series a chart takes next.</summary>
    public required string Second { get; init; }

    /// <summary>The color vision the series are seen in.</summary>
    public required ColorVision Vision { get; init; }

    /// <summary>The distance between the two series as seen, in OKLab, times 100.</summary>
    public required double Distance { get; init; }

    /// <summary>The least distance the series are held to.</summary>
    public required double Target { get; init; }

    /// <summary>Whether the distance meets the target.</summary>
    public bool Meets
        => Distance >= Target;

    /// <inheritdoc />
    public override string ToString()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{First} and {Second}, {ColorSchemeNames.Of(Scheme)}, {Vision}: {Distance:0.0} apart {(Meets ? "meets" : "misses")} {Target:0.#}");
}
