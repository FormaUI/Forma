namespace Forma.Blazor.Browser.Tests;

/// <summary>The components in each runtime a Web App renders them in.</summary>
public sealed class RenderModeTests(BrowserHost host)
{
    /// <summary>The pages and the runtime each first turns interactive in; Auto starts on the Server until WebAssembly has downloaded.</summary>
    public static TheoryData<string, string> Pages { get; } = new()
    {
        { "server", "Server" },
        { "webassembly", "WebAssembly" },
        { "auto", "Server" },
    };

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Should_turn_interactive_in_the_page_s_runtime(string path, string runtime)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;

        // Act
        await page.GotoAsync(path);
        await WaitForInteractiveAsync(page);

        // Assert
        Assert.Equal($"{runtime} True", await page.Locator("#runtime").TextContentAsync());
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Should_bind_text_as_it_is_typed(string path, string runtime)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        await page.GotoAsync(path);
        await WaitForInteractiveAsync(page);

        // Act
        await page.GetByLabel("Name").PressSequentiallyAsync("Ada");

        // Assert
        await Assertions.Expect(page.Locator("#name")).ToHaveTextAsync("Ada");
        Assert.StartsWith(runtime, await page.Locator("#runtime").TextContentAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Should_open_the_select_s_list_through_interop_and_bind_the_choice(string path, string runtime)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        await page.GotoAsync(path);
        await WaitForInteractiveAsync(page);

        // Act
        await page.Locator(".forma-select-trigger-wrapper button").ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Closed" }).ClickAsync();

        // Assert
        await Assertions.Expect(page.Locator("#status")).ToHaveTextAsync("2");
        Assert.StartsWith(runtime, await page.Locator("#runtime").TextContentAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_run_auto_on_webassembly_once_its_resources_downloaded()
    {
        // Arrange — each _framework request is tracked as itself, and the page is ready once none is pending
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        var downloads = new FrameworkDownloads(page);
        await page.GotoAsync("auto");
        await WaitForInteractiveAsync(page);
        await downloads.StopOnceDownloadedAsync();

        // Act
        await page.ReloadAsync();
        await WaitForInteractiveAsync(page);

        // Assert
        Assert.Empty(downloads.Late);
        Assert.Equal("WebAssembly True", await page.Locator("#runtime").TextContentAsync());
    }

    private static async Task WaitForInteractiveAsync(IPage page)
        => await page.WaitForFunctionAsync("() => /True$/.test(document.querySelector('#runtime')?.textContent ?? '')", null, new() { Timeout = 60000 });
}
