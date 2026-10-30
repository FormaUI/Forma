namespace Forma.Blazor.Browser.Tests;

using static Forma.Blazor.Browser.Tests.CulturePages;

/// <summary>The culture under WebAssembly, whose requirements the coordinator's steps meet: known failures until then.</summary>
[Trait("Kind", KnownFailure.Kind)]
public sealed class WebAssemblyCultureTests(BrowserHost host)
{
    [Fact]
    public async Task Should_run_an_event_after_a_webassembly_switch_in_the_new_pair()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        await OpenWebAssemblyPanelAsync(page, "webassembly/culture");
        await page.Locator("#wasm-switch").ClickAsync();
        await Assertions.Expect(page.Locator("#wasm-state")).ToHaveTextAsync("he/he");

        // Act
        await page.Locator("#wasm-read").ClickAsync();

        // Assert
        await KnownFailure.AssertStillFailsAsync(
            "An event after a switch made under WebAssembly runs in the new pair.",
            () => Assertions.Expect(page.Locator("#wasm-ambient")).ToHaveTextAsync("he/he", new() { Timeout = 5000 }));
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
        await KnownFailure.AssertStillFailsAsync(
            "The document follows a switch made under WebAssembly.",
            () => WaitForDocumentAsync(page, "he/rtl", timeout: 5000));
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
        await KnownFailure.AssertStillFailsAsync(
            "A prerendered WebAssembly component starts in the pair the server rendered it in.",
            () => Assertions.Expect(page.Locator("#wasm-first")).ToHaveTextAsync("he/he", new() { Timeout = 5000 }));
    }

    [Fact]
    public async Task Should_reach_both_runtimes_with_a_switch_from_either()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(host.StateAddress);
        await using var _ = context;
        await OpenWebAssemblyPanelAsync(page, "culture/both");
        await WaitForPanelAsync(page);

        // Act
        await page.Locator("#server-island #switch-state").ClickAsync();
        await Assertions.Expect(page.Locator("#server-island #state")).ToHaveTextAsync("he");

        // Assert
        await KnownFailure.AssertStillFailsAsync(
            "With Server and WebAssembly islands on one page, a switch from either reaches both.",
            () => Assertions.Expect(page.Locator("#wasm-state")).ToHaveTextAsync("he/he", new() { Timeout = 5000 }));
    }

    private static async Task OpenWebAssemblyPanelAsync(IPage page, string path)
    {
        await page.GotoAsync(path);
        await Assertions.Expect(page.Locator("#wasm-runtime")).ToHaveTextAsync("WebAssembly True", new() { Timeout = 60000 });
    }
}
