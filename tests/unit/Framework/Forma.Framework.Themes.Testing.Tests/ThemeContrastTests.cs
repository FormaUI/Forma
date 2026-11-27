namespace Forma.Framework.Themes.Testing.Tests;

using Forma.Framework.Css;
using Forma.Framework.Themes;
using Forma.Framework.Themes.Testing;

using Xunit;

public sealed class ThemeContrastTests
{
    // A light scheme meeting every target of the theme design: black text on white backdrops and a white popover, dark solids
    // under white text and a light warning solid under black, each ink a darker step, a light gray quiet hover fill, and
    // Forma's own chart series.
    private static Dictionary<string, CssColor> CreateMeetingColors()
    {
        var colors = new Dictionary<string, CssColor>(StringComparer.Ordinal)
        {
            ["--backdrop-canvas-bg-color"] = CssColor.FromHex(0xffffff),
            ["--backdrop-flat-bg-color"] = CssColor.FromHex(0xffffff),
            ["--backdrop-inset-bg-color"] = CssColor.FromHex(0xffffff),
            ["--backdrop-raised-bg-color"] = CssColor.FromHex(0xffffff),
            ["--popover-bg-color"] = CssColor.FromHex(0xffffff),
            ["--button-quiet-hover-color"] = CssColor.FromHex(0xe5e7eb),
            ["--text-primary-color"] = CssColor.FromHex(0x000000),
            ["--quiet-text-color"] = CssColor.FromHex(0x000000),
            ["--text-secondary-color"] = CssColor.FromHex(0x595959),
            ["--border-emphasis-color"] = CssColor.FromHex(0x767676),
            ["--focus-color"] = CssColor.FromHex(0x003a70),
            ["--chart-series-1-color"] = CssColor.FromHex(0x2a78d6),
            ["--chart-series-2-color"] = CssColor.FromHex(0xeb6834),
            ["--chart-series-3-color"] = CssColor.FromHex(0x1baf7a),
            ["--chart-series-4-color"] = CssColor.FromHex(0xeda100),
        };

        foreach (var (role, solid, emphasis, fg, ink) in (ValueTuple<string, int, int, int, int>[])
        [
            ("primary", 0x003a70, 0x002a52, 0xffffff, 0x002a52),
            ("secondary", 0x3a3a3a, 0x2a2a2a, 0xffffff, 0x2a2a2a),
            ("success", 0x2e7d32, 0x1b5e20, 0xffffff, 0x1b5e20),
            ("danger", 0x8b1a1a, 0x6b1414, 0xffffff, 0x6b1414),
            ("warning", 0xffd54f, 0xe6b800, 0x000000, 0x5c4400),
            ("info", 0x00487a, 0x00365c, 0xffffff, 0x00365c),
        ])
        {
            colors[$"--{role}-color"] = CssColor.FromHex(solid);
            colors[$"--{role}-emphasis-color"] = CssColor.FromHex(emphasis);
            colors[$"--{role}-subtle-color"] = CssColor.FromHex(solid, 0.1);
            colors[$"--{role}-fg-color"] = CssColor.FromHex(fg);
            colors[$"--{role}-ink-color"] = CssColor.FromHex(ink);
        }

        foreach (var (role, hover, active) in (ValueTuple<string, int, int>[])[("primary", 0x002a52, 0x001a33), ("secondary", 0x2a2a2a, 0x1a1a1a), ("danger", 0x6b1414, 0x4b0d0d)])
        {
            colors[$"--button-{role}-hover-color"] = CssColor.FromHex(hover);
            colors[$"--button-{role}-active-color"] = CssColor.FromHex(active);
        }

        return colors;
    }

    private static ThemeColors CreateLightColors(Dictionary<string, CssColor> colors)
        => new("check", ColorSchemes.Light, colors);

    public sealed class Measurement
    {
        [Fact]
        public void Should_measure_every_pair_and_separation_the_targets_name_and_find_each_met()
        {
            // Act
            var report = ThemeContrast.Measure(CreateLightColors(CreateMeetingColors()));

            // Assert — on five surfaces, ten texts, five secondary texts, 60 inks, five pressed toggles and ten boundaries, with
            // 15 fills; three adjacent series in three visions
            Assert.Equal(105, report.Pairs.Count);
            Assert.Equal(9, report.Separations.Count);
            Assert.True(report.MeetsEveryTarget, report.ToString());
        }

        [Fact]
        public void Should_measure_black_text_on_white_paper_at_21_to_1()
        {
            // Act
            var report = ThemeContrast.Measure(CreateLightColors(CreateMeetingColors()));

            // Assert
            var pair = Assert.Single(report.Pairs, pair => pair.Foreground == "--text-primary-color" && pair.Background == "--backdrop-flat-bg-color");
            Assert.Equal(21, pair.Ratio, 6);
            Assert.Equal(7, pair.Target);
        }

        [Fact]
        public void Should_lay_a_translucent_tint_over_the_backdrop_beneath_it()
        {
            // Arrange — a black tint at half its strength over white paper draws an even gray
            var colors = CreateMeetingColors();
            colors["--primary-subtle-color"] = CssColor.FromHex(0x000000, 0.5);
            colors["--primary-ink-color"] = CssColor.FromHex(0xffffff);
            var gray = Math.Pow((0.5 + 0.055) / 1.055, 2.4);

            // Act
            var report = ThemeContrast.Measure(CreateLightColors(colors));

            // Assert
            var pair = Assert.Single(report.Pairs, pair => pair.Background == "--primary-subtle-color" && pair.Backdrop == "--backdrop-flat-bg-color");
            Assert.Equal("--primary-ink-color", pair.Foreground);
            Assert.Equal(1.05 / (gray + 0.05), pair.Ratio, 6);
        }

        [Fact]
        public void Should_refuse_each_pair_and_separation_a_theme_misses_pair_by_pair()
        {
            // Arrange — gray primary text, a pale field border, and a fourth series drawn as the third
            var colors = CreateMeetingColors();
            colors["--text-primary-color"] = CssColor.FromHex(0x777777);
            colors["--border-emphasis-color"] = CssColor.FromHex(0xdddddd);
            colors["--chart-series-4-color"] = CssColor.FromHex(0x1baf7a);
            string[] surfaces = ["--backdrop-canvas-bg-color", "--backdrop-flat-bg-color", "--backdrop-inset-bg-color", "--backdrop-raised-bg-color", "--popover-bg-color"];

            // Act
            var report = ThemeContrast.Measure(CreateLightColors(colors));

            // Assert
            Assert.Equal(
                [.. surfaces.Select(surface => ("--text-primary-color", surface)), .. surfaces.Select(surface => ("--border-emphasis-color", surface))],
                report.MissedPairs.Select(pair => (pair.Foreground, pair.Background)));
            Assert.All(report.MissedPairs, pair => Assert.Null(pair.Backdrop));
            Assert.Equal(
                [ColorVision.Protanopia, ColorVision.Deuteranopia, ColorVision.Full],
                report.MissedSeparations.Select(separation => separation.Vision));
            Assert.All(report.MissedSeparations, separation => Assert.Equal(("--chart-series-3-color", "--chart-series-4-color"), (separation.First, separation.Second)));
            Assert.False(report.MeetsEveryTarget);
        }

        [Fact]
        public void Should_refuse_a_pressed_toggle_s_label_on_a_quiet_hover_fill_it_can_t_be_read_on()
        {
            // Arrange — a dark gray quiet hover fill under the primary ink, which reads on every surface
            var colors = CreateMeetingColors();
            colors["--button-quiet-hover-color"] = CssColor.FromHex(0x555555);

            // Act
            var report = ThemeContrast.Measure(CreateLightColors(colors));

            // Assert
            Assert.Equal(
                ["--backdrop-canvas-bg-color", "--backdrop-flat-bg-color", "--backdrop-inset-bg-color", "--backdrop-raised-bg-color", "--popover-bg-color"],
                report.MissedPairs.Select(pair => pair.Backdrop));
            Assert.All(report.MissedPairs, pair => Assert.Equal(("--primary-ink-color", "--button-quiet-hover-color", 4.5), (pair.Foreground, pair.Background, pair.Target)));
        }

        [Fact]
        public void Should_hold_a_theme_to_a_target_raised_past_the_design_s()
        {
            // Arrange — white on the success solid stands near 5.1:1, past the design's 4.5:1 and short of 7:1
            var targets = ThemeContrastTargets.Default with { RoleForeground = 7 };

            // Act
            var report = ThemeContrast.Measure(CreateLightColors(CreateMeetingColors()), targets);

            // Assert
            var pair = Assert.Single(report.MissedPairs);
            Assert.Equal(("--success-fg-color", "--success-color", "--backdrop-flat-bg-color"), (pair.Foreground, pair.Background, pair.Backdrop));
            Assert.Empty(report.MissedSeparations);
        }

        [Fact]
        public void Should_measure_forma_s_chart_series_apart_as_the_charts_validation_does()
        {
            // Act
            var report = ThemeContrast.Measure(CreateLightColors(CreateMeetingColors()));

            // Assert — the third and fourth series are the closest pair: 9.1 apart in protanopia, 22.9 in full vision
            var closest = report.Separations.Where(separation => separation.First == "--chart-series-3-color").ToDictionary(separation => separation.Vision, separation => separation.Distance);
            Assert.InRange(closest[ColorVision.Protanopia], 9.05, 9.15);
            Assert.InRange(closest[ColorVision.Full], 22.85, 22.95);
        }

        [Fact]
        public void Should_throw_where_the_colors_lack_a_token_the_targets_name()
        {
            // Arrange
            var colors = CreateMeetingColors();
            colors.Remove("--focus-color");

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => ThemeContrast.Measure(CreateLightColors(colors)));

            // Assert
            Assert.Contains("--focus-color", exception.Message, StringComparison.Ordinal);
        }
    }

    public sealed class Colors
    {
        [Theory]
        [InlineData(ColorSchemes.None)]
        [InlineData(ColorSchemes.Light | ColorSchemes.Dark)]
        public void Should_refuse_anything_but_one_scheme(ColorSchemes scheme)
        {
            // Act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ThemeColors("check", scheme, CreateMeetingColors()));

            // Assert
            Assert.Equal("scheme", exception.ParamName);
        }
    }
}
