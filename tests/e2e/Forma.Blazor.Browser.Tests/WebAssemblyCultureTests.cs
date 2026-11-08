namespace Forma.Blazor.Browser.Tests;

using static Forma.Blazor.Browser.Tests.CulturePages;

/// <summary>The culture under WebAssembly: each runtime following the page's coordinator from the pair the server rendered, a client culture declared independent, and a standalone app's page.</summary>
public sealed class WebAssemblyCultureTests(BrowserHost host)
{
    [Fact]
    public async Task Should_run_an_event_after_a_webassembly_switch_and_work_it_schedules_in_the_new_pair()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        await OpenWebAssemblyPanelAsync(page, "webassembly/culture");
        await page.Locator("#wasm-switch").ClickAsync();
        await Assertions.Expect(page.Locator("#wasm-state")).ToHaveTextAsync("he/he");

        // Act
        await page.Locator("#wasm-read").ClickAsync();
        await page.Locator("#wasm-read-later").ClickAsync();

        // Assert
        await Assertions.Expect(page.Locator("#wasm-ambient")).ToHaveTextAsync("he/he");
        await Assertions.Expect(page.Locator("#wasm-later")).ToHaveTextAsync("he/he");
    }

    [Fact]
    public async Task Should_follow_a_webassembly_switch_in_the_document()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        await OpenWebAssemblyPanelAsync(page, "webassembly/culture");

        // Act
        await page.Locator("#wasm-switch").ClickAsync();
        await Assertions.Expect(page.Locator("#wasm-state")).ToHaveTextAsync("he/he");

        // Assert
        await WaitForDocumentAsync(page, "he/rtl");
    }

    [Fact]
    public async Task Should_start_webassembly_in_the_pair_the_server_rendered()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;

        // Act
        await OpenWebAssemblyPanelAsync(page, "he/webassembly/culture");

        // Assert
        await Assertions.Expect(page.Locator("#wasm-first")).ToHaveTextAsync("he/he");
    }

    [Theory]
    [InlineData("#server-island #switch-state", "#wasm-state", "he/he")]
    [InlineData("#webassembly-island #wasm-switch", "#server-island #state", "he")]
    public async Task Should_reach_both_runtimes_with_a_switch_from_either(string switching, string reached, string pair)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(host.StateAddress);
        await using var _ = context;
        await OpenWebAssemblyPanelAsync(page, "culture/both");
        await WaitForPanelAsync(page);

        // Act
        await page.Locator(switching).ClickAsync();

        // Assert
        await Assertions.Expect(page.Locator(reached)).ToHaveTextAsync(pair);
        await WaitForDocumentAsync(page, "he/rtl");
    }

    [Fact]
    public async Task Should_agree_with_the_server_s_next_response_after_a_webassembly_switch_by_address()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        await OpenWebAssemblyPanelAsync(page, "webassembly/culture");
        await page.Locator("#wasm-switch").ClickAsync();
        await Assertions.Expect(page.Locator("#wasm-state")).ToHaveTextAsync("he/he");

        // Act
        await page.ReloadAsync();
        await Assertions.Expect(page.Locator("#wasm-runtime")).ToHaveTextAsync("WebAssembly True", new() { Timeout = 60000 });

        // Assert
        Assert.Contains("/he/", page.Url, StringComparison.Ordinal);
        Assert.Equal("he/rtl", await GetDocumentAsync(page));
        await Assertions.Expect(page.Locator("#wasm-first")).ToHaveTextAsync("he/he");
    }

    [Fact]
    public async Task Should_agree_with_the_server_s_next_response_after_a_webassembly_switch_under_the_cookie_policy()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(host.CookieAddress);
        await using var _ = context;
        await OpenWebAssemblyPanelAsync(page, "webassembly/culture");
        await page.Locator("#wasm-switch").ClickAsync();
        await Assertions.Expect(page.Locator("#wasm-state")).ToHaveTextAsync("he/he");

        // Act
        await page.ReloadAsync();
        await Assertions.Expect(page.Locator("#wasm-runtime")).ToHaveTextAsync("WebAssembly True", new() { Timeout = 60000 });

        // Assert
        Assert.Equal("he/rtl", await GetDocumentAsync(page));
        await Assertions.Expect(page.Locator("#wasm-first")).ToHaveTextAsync("he/he");
    }

    [Fact]
    public async Task Should_run_auto_on_webassembly_in_the_pair_once_its_resources_downloaded()
    {
        // Arrange — the first visit renders on the Server while the runtime downloads
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        var downloads = new FrameworkDownloads(page);
        await page.GotoAsync("he/auto/culture");
        await Assertions.Expect(page.Locator("#wasm-runtime")).ToHaveTextAsync("Server True", new() { Timeout = 60000 });
        await downloads.StopOnceDownloadedAsync();

        // Act
        await page.ReloadAsync();

        // Assert
        await Assertions.Expect(page.Locator("#wasm-runtime")).ToHaveTextAsync("WebAssembly True", new() { Timeout = 60000 });
        await Assertions.Expect(page.Locator("#wasm-first")).ToHaveTextAsync("he/he");
    }

    [Fact]
    public async Task Should_switch_an_independent_client_culture_alone()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(host.IndependentAddress);
        await using var _ = context;
        await OpenWebAssemblyPanelAsync(page, "webassembly/culture");
        var address = page.Url;
        var cookies = GetCookies(await context.CookiesAsync());

        // Act
        await page.Locator("#wasm-switch").ClickAsync();
        await Assertions.Expect(page.Locator("#wasm-state")).ToHaveTextAsync("he/he");
        await page.Locator("#wasm-read").ClickAsync();

        // Assert
        await Assertions.Expect(page.Locator("#wasm-ambient")).ToHaveTextAsync("he/he");
        var document = await GetDocumentAsync(page);
        var cookiesAfter = GetCookies(await context.CookiesAsync());
        Assert.Multiple(
            () => Assert.Equal(address, page.Url),
            () => Assert.Equal("en/ltr", document),
            () => Assert.Equal(cookies, cookiesAfter));
    }

    [Fact]
    public async Task Should_start_a_standalone_app_s_page_from_its_runtime_s_pair_and_follow_its_switch_in_the_host_s_strings_and_the_library_s()
    {
        // Arrange — a browser in German, whose language the standalone runtime starts in
        await using var context = await host.Browser.NewContextAsync(new() { BaseURL = host.StandaloneAddress.ToString(), Locale = "de-DE" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(string.Empty);
        await Assertions.Expect(page.Locator("#standalone-state")).ToHaveTextAsync("de-DE/de-DE", new() { Timeout = 60000 });
        await WaitForDocumentAsync(page, "de-DE/ltr");
        await Assertions.Expect(page.Locator("#standalone-host-string")).ToHaveTextAsync("Host String");
        await Assertions.Expect(page.Locator("#standalone-own-string")).ToHaveTextAsync("Own String");

        // Act
        await page.Locator("#standalone-switch").ClickAsync();

        // Assert
        await Assertions.Expect(page.Locator("#standalone-state")).ToHaveTextAsync("he/he");
        await WaitForDocumentAsync(page, "he/rtl");
        await Assertions.Expect(page.Locator("#standalone-host-string")).ToHaveTextAsync("מחרוזת המארח");
        await Assertions.Expect(page.Locator("#standalone-own-string")).ToHaveTextAsync("מחרוזת הלקוח");
        await Assertions.Expect(page.Locator("nav[aria-label='ניווט עמודים']")).ToHaveCountAsync(1);
    }

    private static string GetCookies(IReadOnlyList<BrowserContextCookiesResult> cookies)
        => string.Join("; ", cookies.Select(cookie => $"{cookie.Name}={cookie.Value}").Order(StringComparer.Ordinal));

    private static async Task OpenWebAssemblyPanelAsync(IPage page, string path)
    {
        await page.GotoAsync(path);
        await Assertions.Expect(page.Locator("#wasm-runtime")).ToHaveTextAsync("WebAssembly True", new() { Timeout = 60000 });
    }
}
