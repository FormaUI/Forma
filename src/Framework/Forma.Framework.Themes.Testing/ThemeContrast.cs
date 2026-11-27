namespace Forma.Framework.Themes.Testing;

using Forma.Framework.Css;

using Microsoft.Playwright;

/// <summary>The contrast check a theme is held to: every pair of tokens the theme design's contrast targets name, in each color scheme, as a browser draws them.</summary>
/// <remarks>
/// The browser draws each token inside an element naming the theme and the scheme, through <c>color-mix(in srgb, …)</c>, so
/// <c>light-dark()</c>, <c>color-mix()</c> and every color space resolve as they draw, and reports it in sRGB. Each pair is then
/// composited as the browser composites: a foreground over its background, a background over the surface it is laid on, every
/// backdrop and the popover's surface over the canvas, and the canvas over white. A role's fills are its solid, its hover fill
/// and its pressed fill: a button's where the role has a button of its own, and otherwise its emphasis. A pressed toggle's label
/// is the primary ink on a quiet button's hover fill. Chart series are seen in protanopia and deuteranopia through Machado,
/// Oliveira and Fernandes' simulations at full severity.
/// </remarks>
public static class ThemeContrast
{
    private const string Canvas = "--backdrop-canvas-bg-color";
    private const string Flat = "--backdrop-flat-bg-color";
    private const string QuietHover = "--button-quiet-hover-color";

    // The color an element inherits where a token draws no color, which no token drawn through color-mix() serializes as.
    private const string Unresolved = "rgb(1, 2, 3)";

    // Every token named, as an element naming the theme and the scheme draws it, or null where it draws no color.
    private const string ReadTokens = """
        ([theme, scheme, tokens, unresolved]) => {
            const scope = document.createElement('div');
            scope.setAttribute('data-forma-theme', theme);
            scope.setAttribute('data-forma-scheme', scheme);
            scope.style.color = unresolved;
            const probe = document.createElement('span');
            scope.append(probe);
            document.body.append(scope);
            try {
                return tokens.map(token => {
                    probe.style.color = `color-mix(in srgb, var(${token}) 100%, transparent)`;
                    const drawn = getComputedStyle(probe).color;
                    return drawn === unresolved ? null : drawn;
                });
            } finally {
                scope.remove();
            }
        }
        """;

    // The surfaces every text, ink and boundary is measured on: the four backdrops and the popover's surface.
    private static readonly string[] Surfaces = [Canvas, Flat, "--backdrop-inset-bg-color", "--backdrop-raised-bg-color", "--popover-bg-color"];

    private static readonly string[] Roles = ["primary", "secondary", "success", "danger", "warning", "info"];

    private static readonly string[] ButtonRoles = ["primary", "secondary", "danger"];

    private static readonly string[] Series = ["--chart-series-1-color", "--chart-series-2-color", "--chart-series-3-color", "--chart-series-4-color"];

    /// <summary>Every token the targets name.</summary>
    public static IReadOnlyList<string> Tokens { get; } =
    [
        .. Surfaces,
        "--text-primary-color",
        "--quiet-text-color",
        "--text-secondary-color",
        .. Roles.SelectMany(role => (string[])[$"--{role}-color", $"--{role}-emphasis-color", $"--{role}-subtle-color", $"--{role}-fg-color", $"--{role}-ink-color"]),
        .. ButtonRoles.SelectMany(role => (string[])[$"--button-{role}-hover-color", $"--button-{role}-active-color"]),
        QuietHover,
        "--border-emphasis-color",
        "--focus-color",
        .. Series,
    ];

    /// <summary>Reads the colors <paramref name="theme"/> draws every token the targets name in, in <paramref name="scheme"/>, on <paramref name="page"/>.</summary>
    /// <param name="page">A page linking Forma's tokens and the theme's stylesheet.</param>
    /// <param name="theme">The theme's name, as <c>data-forma-theme</c> takes it.</param>
    /// <param name="scheme">The color scheme to draw the tokens in, <see cref="ColorSchemes.Light"/> or <see cref="ColorSchemes.Dark"/>.</param>
    /// <returns>The colors the theme draws its tokens in.</returns>
    /// <exception cref="InvalidOperationException">The page draws no color for a token the targets name.</exception>
    public static async Task<ThemeColors> ReadColorsAsync(IPage page, string theme, ColorSchemes scheme)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);

        if (scheme is not (ColorSchemes.Light or ColorSchemes.Dark))
        {
            throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "A theme's colors are read in one scheme, Light or Dark.");
        }

        var drawn = await page.EvaluateAsync<string?[]>(ReadTokens, (object[])[theme, ColorSchemeNames.Of(scheme), Tokens, Unresolved]).ConfigureAwait(false);
        var tokens = new Dictionary<string, CssColor>(StringComparer.Ordinal);
        var undrawn = new List<string>();

        for (var index = 0; index < Tokens.Count; index++)
        {
            if (drawn[index] is { } text)
            {
                tokens[Tokens[index]] = BrowserColors.TryParse(text, out var color)
                    ? color
                    : throw new InvalidOperationException($"The page draws {Tokens[index]} as {text} in the {theme} theme's {ColorSchemeNames.Of(scheme)} scheme, which the check doesn't read as a color.");
            }
            else
            {
                undrawn.Add(Tokens[index]);
            }
        }

        return undrawn.Count == 0
            ? new ThemeColors(theme, scheme, tokens)
            : throw new InvalidOperationException($"The page draws no color for {string.Join(", ", undrawn)} in the {theme} theme's {ColorSchemeNames.Of(scheme)} scheme; it links Forma's tokens and the theme's stylesheet before the theme is measured.");
    }

    /// <summary>Measures every pair and every separation the targets name in <paramref name="colors"/>.</summary>
    /// <param name="colors">The colors a theme draws its tokens in, in one scheme.</param>
    /// <param name="targets">The targets the theme is held to, the theme design's by default.</param>
    /// <returns>The report of every pair and separation in the colors' scheme.</returns>
    /// <exception cref="InvalidOperationException">The colors lack a token the targets name.</exception>
    public static ThemeContrastReport Measure(ThemeColors colors, ThemeContrastTargets? targets = null)
    {
        ArgumentNullException.ThrowIfNull(colors);
        targets ??= ThemeContrastTargets.Default;

        var missing = Tokens.Where(token => !colors.Tokens.ContainsKey(token)).ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"The {colors.Theme} theme's colors lack {string.Join(", ", missing)}, which the targets name.");
        }

        return new ThemeContrastReport(colors.Theme, MeasurePairs(colors, targets), MeasureSeparations(colors, targets));
    }

    /// <summary>Measures every pair and every separation the targets name for <paramref name="theme"/> in each of <paramref name="schemes"/>, on <paramref name="page"/>.</summary>
    /// <param name="page">A page linking Forma's tokens and the theme's stylesheet.</param>
    /// <param name="theme">The theme's name, as <c>data-forma-theme</c> takes it.</param>
    /// <param name="schemes">The color schemes the theme is drawn in.</param>
    /// <param name="targets">The targets the theme is held to, the theme design's by default.</param>
    /// <returns>The report of every pair and separation in each scheme, the light scheme's first.</returns>
    /// <exception cref="InvalidOperationException">The page draws no color for a token the targets name.</exception>
    public static async Task<ThemeContrastReport> MeasureAsync(IPage page, string theme, ColorSchemes schemes, ThemeContrastTargets? targets = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);

        if (schemes == ColorSchemes.None || (schemes & ~(ColorSchemes.Light | ColorSchemes.Dark)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemes), schemes, "A theme is measured in Light, Dark or both.");
        }

        var reports = new List<ThemeContrastReport>();

        foreach (var scheme in (ColorSchemes[])[ColorSchemes.Light, ColorSchemes.Dark])
        {
            if (schemes.HasFlag(scheme))
            {
                reports.Add(Measure(await ReadColorsAsync(page, theme, scheme).ConfigureAwait(false), targets));
            }
        }

        return new ThemeContrastReport(theme, reports.SelectMany(report => report.Pairs), reports.SelectMany(report => report.Separations));
    }

    private static IEnumerable<ContrastPair> MeasurePairs(ThemeColors colors, ThemeContrastTargets targets)
    {
        DrawnColor Read(string token)
        {
            return DrawnColor.From(colors.Tokens[token]);
        }

        var canvas = Read(Canvas).Over(DrawnColor.White);

        DrawnColor DrawSurface(string token)
        {
            return token == Canvas ? canvas : Read(token).Over(canvas);
        }

        ContrastPair Pair(string foreground, string background, string? surface, double target)
        {
            var drawnBackground = surface is null ? DrawSurface(background) : Read(background).Over(DrawSurface(surface));
            var drawnForeground = Read(foreground).Over(drawnBackground);

            return new ContrastPair
            {
                Scheme = colors.Scheme,
                Foreground = foreground,
                Background = background,
                Backdrop = surface,
                Ratio = DrawnColor.Contrast(drawnForeground, drawnBackground),
                Target = target,
            };
        }

        foreach (var text in (string[])["--text-primary-color", "--quiet-text-color"])
        {
            foreach (var surface in Surfaces)
            {
                yield return Pair(text, surface, null, targets.Text);
            }
        }

        foreach (var surface in Surfaces)
        {
            yield return Pair("--text-secondary-color", surface, null, targets.SecondaryText);
        }

        foreach (var role in Roles)
        {
            foreach (var surface in Surfaces)
            {
                yield return Pair($"--{role}-ink-color", surface, null, targets.RoleInk);
                yield return Pair($"--{role}-ink-color", $"--{role}-subtle-color", surface, targets.RoleInk);
            }
        }

        foreach (var surface in Surfaces)
        {
            yield return Pair("--primary-ink-color", QuietHover, surface, targets.RoleInk);
        }

        foreach (var role in Roles)
        {
            string[] fills = ButtonRoles.Contains(role)
                ? [$"--{role}-color", $"--button-{role}-hover-color", $"--button-{role}-active-color"]
                : [$"--{role}-color", $"--{role}-emphasis-color"];

            foreach (var fill in fills)
            {
                yield return Pair($"--{role}-fg-color", fill, Flat, targets.RoleForeground);
            }
        }

        foreach (var boundary in (string[])["--border-emphasis-color", "--focus-color"])
        {
            foreach (var surface in Surfaces)
            {
                yield return Pair(boundary, surface, null, targets.Boundary);
            }
        }
    }

    private static IEnumerable<SeriesSeparation> MeasureSeparations(ThemeColors colors, ThemeContrastTargets targets)
    {
        var surface = DrawnColor.From(colors.Tokens[Flat]).Over(DrawnColor.From(colors.Tokens[Canvas]).Over(DrawnColor.White));

        for (var index = 0; index < Series.Length - 1; index++)
        {
            var first = DrawnColor.From(colors.Tokens[Series[index]]).Over(surface);
            var second = DrawnColor.From(colors.Tokens[Series[index + 1]]).Over(surface);

            foreach (var vision in (ColorVision[])[ColorVision.Protanopia, ColorVision.Deuteranopia, ColorVision.Full])
            {
                yield return new SeriesSeparation
                {
                    Scheme = colors.Scheme,
                    First = Series[index],
                    Second = Series[index + 1],
                    Vision = vision,
                    Distance = DrawnColor.Distance(first, second, vision),
                    Target = vision is ColorVision.Full ? targets.SeriesSeparationInFullVision : targets.SeriesSeparation,
                };
            }
        }
    }
}
