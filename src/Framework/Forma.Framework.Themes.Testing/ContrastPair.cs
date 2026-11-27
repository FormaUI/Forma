namespace Forma.Framework.Themes.Testing;

using System.Globalization;

/// <summary>A foreground token drawn on a background token, held to a least contrast ratio between the two as drawn.</summary>
public sealed record ContrastPair
{
    /// <summary>The color scheme the pair is drawn in.</summary>
    public required ColorSchemes Scheme { get; init; }

    /// <summary>The token drawn on the background: a text, a role's ink or fg, a field's border or the focus ring.</summary>
    public required string Foreground { get; init; }

    /// <summary>The token the foreground is drawn on: a backdrop, a role's subtle tint or one of its fills.</summary>
    public required string Background { get; init; }

    /// <summary>The backdrop the background is laid on, where the background isn't a backdrop itself.</summary>
    public string? Backdrop { get; init; }

    /// <summary>The contrast ratio between the foreground and the background as drawn, from 1 to 21.</summary>
    public required double Ratio { get; init; }

    /// <summary>The least ratio the pair is held to.</summary>
    public required double Target { get; init; }

    /// <summary>Whether the ratio meets the target.</summary>
    public bool Meets
        => Ratio >= Target;

    /// <inheritdoc />
    public override string ToString()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{Foreground} on {Background}{(Backdrop is null ? string.Empty : $" over {Backdrop}")}, {ColorSchemeNames.Of(Scheme)}: {Ratio:0.00}:1 {(Meets ? "meets" : "misses")} {Target:0.##}:1");
}
