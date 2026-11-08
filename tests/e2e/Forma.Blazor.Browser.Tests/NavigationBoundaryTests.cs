namespace Forma.Blazor.Browser.Tests;

/// <summary>What a runtime observes of a navigation under the Server's router and WebAssembly's, which the culture design's interactive landings rest on.</summary>
public sealed class NavigationBoundaryTests(BrowserHost host)
{
    /// <summary>The runtimes whose routers are checked.</summary>
    public static TheoryData<string> Runtimes { get; } = ["Server", "WebAssembly"];

    [Theory]
    [MemberData(nameof(Runtimes))]
    public async Task Should_accept_the_destination_its_router_navigates_to(string runtime)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(runtime));
        await using var _ = context;
        await OpenAsync(page, runtime);

        // Act
        await page.Locator("#go-b").ClickAsync();

        // Assert
        var log = await WaitForLogAsync(page, entries => entries.Contains("start 2 /nav/b") && entries.Contains("accepted 2"));
        var entries = log.ToList();
        Assert.True(entries.IndexOf("start 2 /nav/b") < entries.IndexOf("accepted 2"), string.Join(" | ", log));
    }

    [Theory]
    [MemberData(nameof(Runtimes))]
    public async Task Should_never_accept_a_navigation_a_newer_one_cancelled(string runtime)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(runtime));
        await using var _ = context;
        await OpenAsync(page, runtime);

        // Act — the slow destination waits, and the second navigation supersedes it
        await page.Locator("#go-slow-then-b").ClickAsync();

        // Assert
        var log = await WaitForLogAsync(page, entries => entries.Contains("canceled 2") && entries.Contains("accepted 3"));
        Assert.Multiple(
            () => Assert.Contains("start 2 /nav/slow", log),
            () => Assert.Contains("start 3 /nav/b", log),
            () => Assert.DoesNotContain("accepted 2", log));
    }

    [Theory]
    [MemberData(nameof(Runtimes))]
    public async Task Should_start_no_navigation_a_location_changing_handler_prevented(string runtime)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(runtime));
        await using var _ = context;
        await OpenAsync(page, runtime);
        await page.Locator("#guard").CheckAsync();
        await Assertions.Expect(page.Locator("#guarded")).ToHaveTextAsync("True");

        // Act
        await page.Locator("#link-b").ClickAsync();

        // Assert
        var log = await WaitForLogAsync(page, entries => entries.Contains("prevented /nav/b"));
        Assert.Multiple(
            () => Assert.DoesNotContain(log, entry => entry.StartsWith("start 2", StringComparison.Ordinal)),
            () => Assert.EndsWith("/nav/a", new Uri(page.Url).AbsolutePath, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Runtimes))]
    public async Task Should_navigate_back_and_forward_through_the_router(string runtime)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(runtime));
        await using var _ = context;
        await OpenAsync(page, runtime);
        await page.Locator("#go-b").ClickAsync();
        await WaitForLogAsync(page, entries => entries.Contains("accepted 2"));

        // Act
        await page.GoBackAsync();
        await WaitForLogAsync(page, entries => entries.Contains("accepted 3"));
        await page.GoForwardAsync();

        // Assert
        var log = await WaitForLogAsync(page, entries => entries.Contains("accepted 4"));
        Assert.Multiple(
            () => Assert.Contains("location /nav/a intercepted=True", log),
            () => Assert.Contains("start 3 /nav/a", log),
            () => Assert.Contains("start 4 /nav/b", log));
    }

    [Theory]
    [MemberData(nameof(Runtimes))]
    public async Task Should_land_on_no_page_for_an_address_no_route_names(string runtime)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(runtime));
        await using var _ = context;
        await OpenAsync(page, runtime);

        // Act
        await page.Locator("#go-missing").ClickAsync();

        // Assert
        await WaitForLogAsync(page, entries => entries.Contains("accepted 2"));
        await Assertions.Expect(page.Locator("#nav-page")).ToHaveTextAsync("not found");
    }

    [Theory]
    [MemberData(nameof(Runtimes))]
    public async Task Should_start_a_new_runtime_for_a_navigation_that_replaces_the_document(string runtime)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(runtime));
        await using var _ = context;
        await OpenAsync(page, runtime);
        var before = await page.Locator("#nav-runtime").TextContentAsync();

        // Act
        await page.Locator("#reload-b").ClickAsync();
        await Assertions.Expect(page.Locator("#nav-runtime")).Not.ToHaveTextAsync(before!, new() { Timeout = 60000 });
        await Assertions.Expect(page.Locator("#nav-runtime")).ToContainTextAsync(runtime, new() { Timeout = 60000 });

        // Assert
        var log = await WaitForLogAsync(page, entries => entries.Contains("accepted 1"));
        Assert.Equal(["start 1 /nav/b", "accepted 1"], log);
    }

    private Task<Uri> GetAddressAsync(string runtime)
        => runtime == "Server" ? host.GetNavigationServerAddressAsync() : host.GetNavigationWebAssemblyAddressAsync();

    private static async Task OpenAsync(IPage page, string runtime)
    {
        await page.GotoAsync("nav/a");
        await Assertions.Expect(page.Locator("#nav-runtime")).ToContainTextAsync(runtime, new() { Timeout = 60000 });
        await WaitForLogAsync(page, entries => entries.Contains("accepted 1"));
    }

    private static async Task<IReadOnlyList<string>> WaitForLogAsync(IPage page, Func<IReadOnlyList<string>, bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        while (true)
        {
            var entries = await page.Locator("#nav-log li").AllTextContentsAsync();

            if (condition(entries))
            {
                return entries;
            }

            await page.WaitForFunctionAsync("count => document.querySelectorAll('#nav-log li').length !== count", entries.Count, new() { Timeout = 30000 });
            timeout.Token.ThrowIfCancellationRequested();
        }
    }
}
