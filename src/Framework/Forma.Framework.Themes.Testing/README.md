# Forma.Framework.Themes.Testing

The contrast check a host's tests hold its themes to. It measures every pair of tokens the
[theme design's contrast targets](https://github.com/FormaUI/Forma/blob/main/docs/theme-design.md#contrast) name, in each
color scheme, as a browser draws them, and reports each pair with its ratio and its target. It drives the browser through
[Playwright](https://playwright.dev/dotnet/), so it belongs in a test project, never in an application that only draws
themes.

## Install

```pwsh
dotnet add package formaui-net.Framework.Themes.Testing --prerelease
```

## Checking a theme

The check runs on a Playwright page that links Forma's tokens and the theme's stylesheet: a page of the host's own
application, or one the test writes. It draws each token inside an element naming the theme and the scheme, measures each
pair, and leaves the page as it found it.

```csharp
using Forma.Framework.Themes;
using Forma.Framework.Themes.Testing;

[Fact]
public async Task Should_hold_the_harbor_theme_to_forma_s_contrast_targets()
{
    // Arrange — a page of the application, which links forma-tokens.css and harbor-theme.css
    await page.GotoAsync(applicationAddress);

    // Act
    var report = await ThemeContrast.MeasureAsync(page, "harbor", ColorSchemes.Light | ColorSchemes.Dark);

    // Assert
    Assert.True(report.MeetsEveryTarget, report.ToString());
}
```

A theme with a light scheme alone is measured in `ColorSchemes.Light`. A page that draws no color for a token the targets
name, because it links neither Forma's tokens nor the theme, throws `InvalidOperationException` naming the tokens.

## What the check measures

| Pair | Target |
| --- | --- |
| Primary text and quiet text on each backdrop | 7:1 |
| Secondary text on each backdrop | 4.5:1 |
| Each role's ink on each backdrop and on its subtle tint | 4.5:1 |
| Each role's fg on its solid, its hover fill and its pressed fill | 4.5:1 |
| A field's border, and the focus ring, on each backdrop | 3:1 |
| Adjacent chart series in protanopia and deuteranopia | 8 apart |
| Adjacent chart series in full color vision | 15 apart |

A ratio is WCAG's contrast ratio, and a distance between series is their distance in OKLab, times 100. Each pair is
measured as it's drawn, a translucent token laid over what it stands on.

## The report

| Member | What it is |
| --- | --- |
| `Pairs` | Every `ContrastPair` measured: its scheme, its foreground and background tokens, the backdrop beneath a translucent background, its ratio and its target |
| `Separations` | Every `SeriesSeparation` measured: its scheme, its two series, the `ColorVision` they're seen in, their distance and its target |
| `MissedPairs`, `MissedSeparations` | Those that miss their targets |
| `MeetsEveryTarget` | Whether nothing missed |
| `ToString()` | The theme and each miss, one to a line |

## Raising the targets

`ThemeContrastTargets` holds every target, the design's by default, and a theme can be held to higher ones, such as AAA's
7:1 for the text on its solids:

```csharp
var targets = ThemeContrastTargets.Default with { RoleForeground = 7 };
var report = await ThemeContrast.MeasureAsync(page, "harbor", ColorSchemes.Light | ColorSchemes.Dark, targets);
```

`ThemeContrast.ReadColorsAsync` reads the colors a theme draws in one scheme, and `ThemeContrast.Measure` measures colors
read before, or written by the test, with no browser.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
