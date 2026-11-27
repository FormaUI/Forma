namespace Forma.Framework.Themes.Testing;

using System.Globalization;

/// <summary>What a theme's contrast check measured: every pair and every separation its targets name, in each color scheme it measured.</summary>
public sealed class ThemeContrastReport
{
    internal ThemeContrastReport(string theme, IEnumerable<ContrastPair> pairs, IEnumerable<SeriesSeparation> separations)
    {
        Theme = theme;
        Pairs = [.. pairs];
        Separations = [.. separations];
        MissedPairs = [.. Pairs.Where(pair => !pair.Meets)];
        MissedSeparations = [.. Separations.Where(separation => !separation.Meets)];
    }

    /// <summary>The theme's name.</summary>
    public string Theme { get; }

    /// <summary>Every pair measured, in the order of the targets.</summary>
    public IReadOnlyList<ContrastPair> Pairs { get; }

    /// <summary>Every separation of adjacent chart series measured.</summary>
    public IReadOnlyList<SeriesSeparation> Separations { get; }

    /// <summary>The pairs that miss their targets.</summary>
    public IReadOnlyList<ContrastPair> MissedPairs { get; }

    /// <summary>The separations that miss their targets.</summary>
    public IReadOnlyList<SeriesSeparation> MissedSeparations { get; }

    /// <summary>Whether every pair and every separation meets its target.</summary>
    public bool MeetsEveryTarget
        => MissedPairs.Count == 0 && MissedSeparations.Count == 0;

    /// <inheritdoc />
    public override string ToString()
        => MeetsEveryTarget
            ? string.Create(CultureInfo.InvariantCulture, $"The {Theme} theme meets every target.")
            : string.Join(
                Environment.NewLine,
                [
                    string.Create(CultureInfo.InvariantCulture, $"The {Theme} theme misses {MissedPairs.Count + MissedSeparations.Count} targets:"),
                    .. MissedPairs.Select(pair => $"  {pair}"),
                    .. MissedSeparations.Select(separation => $"  {separation}"),
                ]);
}
