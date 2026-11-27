namespace Forma.Blazor.Browser.Tests.Components;

using System.Text.RegularExpressions;

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

    // Takes the theme and the scheme the host's page names off its root, so the page names neither.
    private const string NameNeitherOnTheRoot = "() => { delete document.documentElement.dataset.formaTheme; delete document.documentElement.dataset.formaScheme; }";

    // Whether the check themes' stylesheets and Forma High Contrast's have reached the page.
    private const string AreThemesLoaded = "() => ['check-theme.css', 'contrast-themes.css', 'forma-high-contrast.css'].every(name => [...document.styleSheets].some(sheet => sheet.href?.endsWith(name)))";

    public sealed class Scopes(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_every_token_in_the_theme_and_the_scheme_its_nearest_scope_names(string runtime)
        {
            // Arrange — a reference for each theme in each scheme, each drawn apart from the others, on a page whose root names neither
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);
            await page.EvaluateAsync(NameNeitherOnTheRoot);
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
            // Arrange — a page whose root names neither a theme nor a scheme
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);
            await page.EvaluateAsync(NameNeitherOnTheRoot);
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_a_host_theme_named_on_the_root_and_forma_s_in_a_part_naming_forma_inside_it(string runtime)
        {
            // Arrange — the references, then the root naming the check theme in its dark scheme
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);
            var formaDark = await ReadTokensAsync(page, "forma-dark");
            var checkDark = await ReadTokensAsync(page, "check-dark");
            await page.EvaluateAsync("() => { document.documentElement.dataset.formaTheme = 'check'; document.documentElement.dataset.formaScheme = 'dark'; }");
            var problems = new List<string>();

            // Act
            problems.AddRange(Compare("unthemed", await ReadTokensAsync(page, "unthemed"), "check-dark", checkDark));
            problems.AddRange(Compare("forma-in-root", await ReadTokensAsync(page, "forma-in-root"), "forma-dark", formaDark));

            // Assert — the root draws the check theme's every token, none of the ones Forma's theme sets past its base tokens
            Assert.NotEqual(formaDark["--primary-ink-color"], checkDark["--primary-ink-color"]);
            Assert.Empty(problems);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_a_scope_in_the_scheme_its_parameter_changes_to(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);
            var checkLight = await ReadTokensAsync(page, "check-light");
            var checkDark = await ReadTokensAsync(page, "check-dark");
            var problems = Compare("switched", await ReadTokensAsync(page, "switched"), "check-light", checkLight).ToList();

            // Act
            await page.Locator("#switch-scheme").ClickAsync();
            await page.WaitForFunctionAsync("() => document.getElementById('switched').parentElement.dataset.formaScheme === 'dark'");
            problems.AddRange(Compare("switched", await ReadTokensAsync(page, "switched"), "check-dark", checkDark));

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
        public async Task Should_hold_forma_s_theme_to_every_target_in_both_schemes(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;

            // Act
            var report = await ThemeContrast.MeasureAsync(page, "forma", ColorSchemes.Light | ColorSchemes.Dark);

            // Assert
            Assert.True(report.MeetsEveryTarget, report.ToString());
        }

        [Theory]
        [MemberData(nameof(EngineRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hold_forma_high_contrast_to_7_to_1_for_every_text_ink_and_fg_in_both_schemes(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;
            await page.WaitForFunctionAsync(AreThemesLoaded);
            var targets = ThemeContrastTargets.Default with { SecondaryText = 7, RoleInk = 7, RoleForeground = 7 };

            // Act
            var report = await ThemeContrast.MeasureAsync(page, "forma-high-contrast", ColorSchemes.Light | ColorSchemes.Dark, targets);

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

    public sealed partial class FirstPaint(BrowserHost host)
    {
        // The two attributes on <html>, and the theme marker's attributes beside them, in the document as the browser parsed it.
        private const string ReadDocument = """
            () => ({
                ...Object.fromEntries([...document.querySelector('meta[name="forma-theme"]').attributes].map(attribute => [`marker-${attribute.name}`, attribute.value])),
                theme: document.documentElement.dataset.formaTheme ?? '',
                scheme: document.documentElement.dataset.formaScheme ?? '',
            })
            """;

        public static TheoryData<string> Engines { get; } = ["chromium", "webkit"];

        [Theory]
        [MemberData(nameof(Engines))]
        public async Task Should_draw_a_statically_rendered_page_in_the_choice_its_record_names_from_its_first_paint(string engine)
        {
            // Arrange
            var (context, page, address) = await OpenAsync(engine);
            await using var _ = context;
            await context.AddCookiesAsync([new() { Name = "forma-theme", Value = "theme=check&scheme=dark&revision=4", Url = address.ToString() }]);

            // Act
            var response = await page.GotoAsync("static/themes");
            var served = await response!.TextAsync();
            var (theme, scheme, marker) = await ReadDocumentAsync(page);
            var unthemed = await ReadTokensAsync(page, "unthemed");
            var checkDark = await ReadTokensAsync(page, "check-dark");

            // Assert — written by the server, so drawn from the first paint, and the page's tokens the check theme's dark ones
            Assert.Multiple(
                () => Assert.Matches(DocumentElement("check", "dark"), served),
                () => Assert.Equal(("check", "dark"), (theme, scheme)),
                () => Assert.Equal(("check", "dark", "4"), (marker["data-theme"], marker["data-scheme"], marker["data-revision"])),
                () => Assert.False(marker.ContainsKey("data-keeps-nothing")),
                () => Assert.Equal(checkDark["--backdrop-flat-bg-color"], unthemed["--backdrop-flat-bg-color"]),
                () => Assert.Equal(checkDark["--primary-ink-color"], unthemed["--primary-ink-color"]));
        }

        [Theory]
        [MemberData(nameof(Engines))]
        public async Task Should_draw_a_page_without_a_record_in_the_default_and_one_naming_a_removed_theme_in_the_default_theme_in_its_scheme(string engine)
        {
            // Arrange
            var (context, page, address) = await OpenAsync(engine);
            await using var _ = context;
            await page.GotoAsync("static/themes");
            var (unrecordedTheme, unrecordedScheme, unrecordedMarker) = await ReadDocumentAsync(page);
            await context.AddCookiesAsync([new() { Name = "forma-theme", Value = "theme=removed&scheme=dark&revision=2", Url = address.ToString() }]);

            // Act
            await page.GotoAsync("static/themes");
            var (removedTheme, removedScheme, _) = await ReadDocumentAsync(page);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(("forma", "system", "0"), (unrecordedTheme, unrecordedScheme, unrecordedMarker["data-revision"])),
                () => Assert.Equal(("forma", "dark"), (removedTheme, removedScheme)));
        }

        [Theory]
        [MemberData(nameof(Engines))]
        public async Task Should_draw_a_request_given_its_choice_in_it_writing_no_record_and_marking_its_page_as_keeping_nothing(string engine)
        {
            // Arrange — the reader's own choice in the record
            var (context, page, address) = await OpenAsync(engine);
            await using var _ = context;
            await context.AddCookiesAsync([new() { Name = "forma-theme", Value = "theme=forma&scheme=light&revision=1", Url = address.ToString() }]);

            // Act
            var response = await page.GotoAsync("static/themes?given-theme=check");
            var (theme, scheme, marker) = await ReadDocumentAsync(page);
            var cookies = await context.CookiesAsync([address.ToString()]);
            var recorded = await response!.HeaderValueAsync("set-cookie");

            // Assert
            Assert.Multiple(
                () => Assert.Equal(("check", "dark"), (theme, scheme)),
                () => Assert.True(marker.ContainsKey("data-keeps-nothing")),
                () => Assert.DoesNotContain("forma-theme=", recorded ?? string.Empty, StringComparison.Ordinal),
                () => Assert.Equal("theme=forma&scheme=light&revision=1", Uri.UnescapeDataString(Assert.Single(cookies, cookie => cookie.Name == "forma-theme").Value)));
        }

        [Theory]
        [MemberData(nameof(Engines))]
        public async Task Should_keep_a_choice_a_statically_rendered_page_posts_and_draw_the_page_it_returns_to_in_it(string engine)
        {
            // Arrange
            var (context, page, address) = await OpenAsync(engine);
            await using var _ = context;
            await page.GotoAsync("static/themes");

            // Act
            await page.Locator("#switch-theme button").ClickAsync();
            await page.WaitForFunctionAsync("() => document.documentElement.dataset.formaTheme === 'check'");
            var (theme, scheme, marker) = await ReadDocumentAsync(page);
            var cookies = await context.CookiesAsync([address.ToString()]);

            // Assert — no store is registered, so the record's revision advances past the one before it
            Assert.Multiple(
                () => Assert.EndsWith("/static/themes", page.Url, StringComparison.Ordinal),
                () => Assert.Equal(("check", "light", "1"), (theme, scheme, marker["data-revision"])),
                () => Assert.Equal("theme=check&scheme=light&revision=1", Uri.UnescapeDataString(Assert.Single(cookies, cookie => cookie.Name == "forma-theme").Value)));
        }

        private async Task<(IBrowserContext Context, IPage Page, Uri Address)> OpenAsync(string engine)
        {
            var address = await host.GetComponentsAddressAsync();
            var (context, page) = engine == "webkit" ? await host.OpenWebKitPageAsync(address) : await host.OpenPageAsync(address);

            return (context, page, address);
        }

        private static Regex DocumentElement(string theme, string scheme)
            => new($"""<html[^>]*\sdata-forma-theme="{theme}"[^>]*\sdata-forma-scheme="{scheme}" """.TrimEnd());

        private static Task<Dictionary<string, string>> ReadTokensAsync(IPage page, string id)
            => page.EvaluateAsync<Dictionary<string, string>>(ReadTokens, id);

        private static async Task<(string Theme, string Scheme, Dictionary<string, string> Marker)> ReadDocumentAsync(IPage page)
        {
            var drawn = await page.EvaluateAsync<Dictionary<string, string>>(ReadDocument);

            return (drawn["theme"], drawn["scheme"], drawn.Where(entry => entry.Key.StartsWith("marker-", StringComparison.Ordinal)).ToDictionary(entry => entry.Key["marker-".Length..], entry => entry.Value));
        }
    }

    public sealed class Switching(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(RoutedRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_switch_the_document_and_the_runtime_in_place_keeping_the_choice_in_the_browser_s_record(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "themes");
            await using var _ = context;

            // Act
            await page.Locator("#switch-check-dark").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#theme-outcome")).ToHaveTextAsync("accepted");
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-forma-theme", "check");
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-forma-scheme", "dark");
            await Assertions.Expect(page.Locator("#theme-choice")).ToHaveTextAsync("check dark");
            var cookies = await context.CookiesAsync();
            Assert.Equal("theme=check&scheme=dark&revision=0", Uri.UnescapeDataString(Assert.Single(cookies, cookie => cookie.Name == "forma-theme").Value));
        }

        [Theory]
        [MemberData(nameof(RoutedRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_accept_a_switch_at_once_on_a_page_given_its_choice_keeping_nothing(string runtime)
        {
            // Arrange — a page the host gives Forma's theme in its dark scheme
            var (context, page) = await OpenAsync(host, runtime, "themes?given-theme=forma");
            await using var _ = context;

            // Act
            await page.Locator("#switch-check-dark").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#theme-outcome")).ToHaveTextAsync("accepted");
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-forma-theme", "check");
            var cookies = await context.CookiesAsync();
            Assert.DoesNotContain(cookies, cookie => cookie.Name == "forma-theme");
        }

        [Theory]
        [MemberData(nameof(RoutedRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_refuse_a_switch_to_a_theme_whose_stylesheet_didn_t_load_changing_nothing(string runtime)
        {
            // Arrange — the check theme's stylesheet refused
            var isRefused = false;
            var (context, page) = await OpenAsync(host, runtime, "themes", page => page.RouteAsync("**/check-theme.css*", route =>
            {
                isRefused = true;
                return route.AbortAsync();
            }));
            await using var _ = context;

            // Act
            await page.Locator("#switch-check-dark").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#theme-outcome")).ToHaveTextAsync(nameof(InvalidOperationException));
            Assert.True(isRefused);
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-forma-theme", "forma");
            var cookies = await context.CookiesAsync();
            Assert.DoesNotContain(cookies, cookie => cookie.Name == "forma-theme");
        }
    }

    public sealed class StandaloneFirstPaint(BrowserHost host)
    {
        // The two attributes the bootstrap set on <html> before the body was parsed.
        private const string ReadDocument = "() => `${document.documentElement.dataset.formaTheme}/${document.documentElement.dataset.formaScheme}`";

        // A record naming a loaded theme, none, one naming a theme the page doesn't link, and one naming a theme whose stylesheet failed.
        public static TheoryData<string?, string> Records { get; } = new()
        {
            { "theme=check&scheme=dark&revision=2", "check/dark" },
            { null, "forma/system" },
            { "theme=removed&scheme=light&revision=1", "forma/light" },
            { "theme=missing&scheme=dark&revision=1", "forma/dark" },
        };

        [Theory]
        [MemberData(nameof(Records))]
        public async Task Should_draw_a_standalone_app_s_page_in_the_choice_its_record_in_local_storage_names_from_its_first_paint(string? value, string drawn)
        {
            // Arrange — the record set before the page's own scripts run
            var (context, page) = await host.OpenPageAsync(await host.GetComponentsAddressAsync());
            await using var _ = context;

            if (value is not null)
            {
                await page.AddInitScriptAsync($"localStorage.setItem('forma-theme', '{value}')");
            }

            // Act
            await page.GotoAsync("_content/Forma.Blazor.Browser.Components.Checks/standalone-themes.html");

            // Assert
            Assert.Equal(drawn, await page.EvaluateAsync<string>(ReadDocument));
        }

        [Fact]
        public async Task Should_draw_a_standalone_app_s_page_in_the_default_where_the_browser_refuses_to_open_storage()
        {
            // Arrange — storage the browser refuses, as a private window's can
            var (context, page) = await host.OpenPageAsync(await host.GetComponentsAddressAsync());
            await using var _ = context;
            await page.AddInitScriptAsync("Object.defineProperty(window, 'localStorage', { get() { throw new DOMException('The storage is refused.', 'SecurityError'); } })");

            // Act
            await page.GotoAsync("_content/Forma.Blazor.Browser.Components.Checks/standalone-themes.html");

            // Assert
            Assert.Equal("forma/system", await page.EvaluateAsync<string>(ReadDocument));
        }
    }
}

