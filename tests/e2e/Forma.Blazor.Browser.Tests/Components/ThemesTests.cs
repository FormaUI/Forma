namespace Forma.Blazor.Browser.Tests.Components;

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

    // Whether the check theme's stylesheet has reached the page, which draws its radius at zero.
    private const string IsCheckThemeLoaded = "() => getComputedStyle(document.getElementById('check-light')).getPropertyValue('--radius-sm').trim() === '0'";

    public sealed class Scopes(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_every_token_in_the_theme_and_the_scheme_its_nearest_scope_names(string runtime)
        {
            // Arrange — a reference for each theme in each scheme, each drawn apart from the others
            var (context, page) = await OpenAsync(host, runtime, "theme-scopes");
            await using var _ = context;
            await page.WaitForFunctionAsync(IsCheckThemeLoaded);
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
            var (context, page) = await OpenAsync(host, runtime, "theme-scopes");
            await using var _ = context;
            await page.WaitForFunctionAsync(IsCheckThemeLoaded);
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
}
