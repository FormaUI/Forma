namespace Forma.Blazor.Browser.Tests.Components;

using Forma.Framework.Themes;
using Forma.Framework.Themes.Testing;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The design tokens drawn in each theme and color scheme a page or a scope inside it names, in every runtime.</summary>
public sealed class ThemesTests
{
    // Every token Forma's token stylesheet declares, as a span inside the element id names draws it: a color as its color, a
    // shadow or a focus ring as its shadow, and any other token as its value.
    private const string ReadTokens = """
        id => {
            const sheet = [...document.styleSheets].find(sheet => /\/forma-tokens(\.\w+)?\.css$/.test(new URL(sheet.href ?? '', location.href).pathname));
            const names = new Set();
            const collect = rules => {
                for (const rule of rules) {
                    for (const name of rule.style ?? []) {
                        if (name.startsWith('--')) {
                            names.add(name);
                        }
                    }

                    collect(rule.cssRules ?? []);
                }
            };
            collect(sheet.cssRules);
            const probe = document.createElement('span');
            document.getElementById(id).append(probe);
            const drawn = {};
            for (const name of names) {
                probe.removeAttribute('style');
                if (name.endsWith('-color')) {
                    probe.style.color = `var(${name})`;
                    drawn[name] = getComputedStyle(probe).color;
                } else if (/^--(shadow|focus-ring)/.test(name)) {
                    probe.style.boxShadow = `var(${name})`;
                    drawn[name] = getComputedStyle(probe).boxShadow;
                } else {
                    drawn[name] = getComputedStyle(probe).getPropertyValue(name).trim();
                }
            }
            probe.remove();
            return drawn;
        }
        """;

    // Whether both check themes' stylesheets have reached the page.
    private const string AreThemesLoaded = "() => ['check-theme.css', 'contrast-themes.css'].every(name => [...document.styleSheets].some(sheet => sheet.href?.endsWith(name)))";

    public sealed class Scopes(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_every_token_in_the_theme_and_the_scheme_its_nearest_scope_names(string runtime)
        {
            // Arrange — a reference for each theme in each scheme, each drawn apart from the others
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);
            var formaLight = await ReadTokensAsync(page, "forma-light");
            var formaDark = await ReadTokensAsync(page, "forma-dark");
            var checkLight = await ReadTokensAsync(page, "check-light");
            var checkDark = await ReadTokensAsync(page, "check-dark");
            Assert.NotEqual(formaLight["--shadow-color"], formaDark["--shadow-color"]);
            Assert.NotEqual(formaLight["--backdrop-flat-bg-color"], checkLight["--backdrop-flat-bg-color"]);
            Assert.NotEqual(checkLight["--backdrop-flat-bg-color"], checkDark["--backdrop-flat-bg-color"]);
            var cases = new (string Id, string Reference, IReadOnlyDictionary<string, string> Tokens)[]
            {
                ("unthemed", "forma-light", formaLight),
                ("outer-light", "check-light", checkLight),
                ("middle-dark", "check-dark", checkDark),
                ("inner-light", "check-light", checkLight),
                ("scheme-only", "check-dark", checkDark),
                ("theme-only", "forma-light", formaLight),
                ("forma-in-check", "forma-dark", formaDark),
                ("check-in-forma", "check-dark", checkDark),
            };
            var problems = new List<string>();

            // Act
            foreach (var (id, reference, tokens) in cases)
            {
                problems.AddRange(Compare(id, await ReadTokensAsync(page, id), reference, tokens));
            }

            // Assert
            Assert.Empty(problems);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_a_system_scope_in_the_scheme_the_device_prefers_and_a_page_naming_no_scheme_light(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);
            var problems = new List<string>();

            // Act — the device preferring each scheme in turn
            foreach (var (preference, scheme) in new[] { (ColorScheme.Light, "light"), (ColorScheme.Dark, "dark") })
            {
                await page.EmulateMediaAsync(new() { ColorScheme = preference });
                await page.WaitForFunctionAsync($"() => matchMedia('(prefers-color-scheme: {scheme})').matches");
                problems.AddRange(Compare("system-in-dark", await ReadTokensAsync(page, "system-in-dark"), $"check-{scheme}", await ReadTokensAsync(page, $"check-{scheme}")));
                problems.AddRange(Compare("unthemed", await ReadTokensAsync(page, "unthemed"), "forma-light", await ReadTokensAsync(page, "forma-light")));
            }

            // Assert
            Assert.Empty(problems);
        }

        private static Task<Dictionary<string, string>> ReadTokensAsync(IPage page, string id)
            => page.EvaluateAsync<Dictionary<string, string>>(ReadTokens, id);

        private static IEnumerable<string> Compare(string id, IReadOnlyDictionary<string, string> drawn, string reference, IReadOnlyDictionary<string, string> expected)
            => expected.Where(token => !string.Equals(drawn.GetValueOrDefault(token.Key), token.Value, StringComparison.Ordinal))
                .Select(token => $"#{id} draws {token.Key} as {drawn.GetValueOrDefault(token.Key)}, where #{reference} draws {token.Value}");
    }

    public sealed class Contrast(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(EngineRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_read_each_token_as_the_theme_draws_it_in_each_scheme(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);

            // Act
            var light = await ThemeContrast.ReadColorsAsync(page, "check", ColorSchemes.Light);
            var dark = await ThemeContrast.ReadColorsAsync(page, "check", ColorSchemes.Dark);

            // Assert — a value written for each scheme, one derived by name, and one cut from the solid by color-mix()
            Assert.Equal("#fffdf7", light.Tokens["--backdrop-flat-bg-color"].ToHexString());
            Assert.Equal("#15171b", dark.Tokens["--backdrop-flat-bg-color"].ToHexString());
            Assert.Equal("#0e1013", dark.Tokens["--backdrop-canvas-bg-color"].ToHexString());
            Assert.Equal(light.Tokens["--primary-color"].ToHexString(), light.Tokens["--primary-subtle-color"].ToHexString());
            Assert.Equal(0.1, light.Tokens["--primary-subtle-color"].Alpha, 3);
            Assert.Equal("#d49be3", dark.Tokens["--primary-color"].ToHexString());
        }

        [Theory]
        [MemberData(nameof(EngineRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hold_forma_s_theme_to_every_target_in_its_light_scheme(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;

            // Act
            var report = await ThemeContrast.MeasureAsync(page, "forma", ColorSchemes.Light);

            // Assert
            Assert.True(report.MeetsEveryTarget, report.ToString());
        }

        [Theory]
        [MemberData(nameof(EngineRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_find_every_target_met_by_a_theme_that_meets_them_in_both_schemes(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);

            // Act
            var report = await ThemeContrast.MeasureAsync(page, "meets", ColorSchemes.Light | ColorSchemes.Dark);

            // Assert
            Assert.Equal(210, report.Pairs.Count);
            Assert.True(report.MeetsEveryTarget, report.ToString());
        }

        [Theory]
        [MemberData(nameof(EngineRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_refuse_each_pair_a_theme_misses_in_the_scheme_it_misses_it_in(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);
            string[] surfaces = ["--backdrop-canvas-bg-color", "--backdrop-flat-bg-color", "--backdrop-inset-bg-color", "--backdrop-raised-bg-color", "--popover-bg-color"];
            (ColorSchemes, string, string, string?)[] expected =
            [
                .. surfaces.Select(surface => (ColorSchemes.Light, "--text-primary-color", surface, (string?)null)),
                .. surfaces.SelectMany(surface => ((ColorSchemes, string, string, string?)[])
                [
                    (ColorSchemes.Light, "--success-ink-color", surface, null),
                    (ColorSchemes.Light, "--success-ink-color", "--success-subtle-color", surface),
                ]),
                .. surfaces.Select(surface => (ColorSchemes.Light, "--border-emphasis-color", surface, (string?)null)),
                .. surfaces.Select(surface => (ColorSchemes.Dark, "--text-secondary-color", surface, (string?)null)),
                .. surfaces.Select(surface => (ColorSchemes.Dark, "--primary-ink-color", "--button-quiet-hover-color", (string?)surface)),
                .. ((string[])["--danger-color", "--button-danger-hover-color", "--button-danger-active-color"]).Select(fill => (ColorSchemes.Dark, "--danger-fg-color", fill, (string?)"--backdrop-flat-bg-color")),
                .. surfaces.Select(surface => (ColorSchemes.Dark, "--focus-color", surface, (string?)null)),
            ];

            // Act
            var report = await ThemeContrast.MeasureAsync(page, "misses", ColorSchemes.Light | ColorSchemes.Dark);

            // Assert
            Assert.Equal(
                expected.Order(),
                report.MissedPairs.Select(pair => (pair.Scheme, pair.Foreground, pair.Background, pair.Backdrop)).Order());
            Assert.Equal(
                [(ColorSchemes.Light, ColorVision.Protanopia), (ColorSchemes.Light, ColorVision.Deuteranopia), (ColorSchemes.Light, ColorVision.Full)],
                report.MissedSeparations.Select(separation => (separation.Scheme, separation.Vision)));
            Assert.All(report.MissedSeparations, separation => Assert.Equal("--chart-series-4-color", separation.Second));
        }
    }
}
