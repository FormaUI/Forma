namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The bars' interactions, in every runtime.</summary>
public sealed class BarsTests
{
    public sealed class SearchToolbar(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_report_its_search_and_its_command(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "search-toolbar");
            await using var _ = context;
            var toolbar = page.Locator("#search-toolbar");

            // Act
            await toolbar.GetByRole(AriaRole.Searchbox, new() { Name = "Search Orders", Exact = true }).FillAsync("invoices");
            await toolbar.GetByRole(AriaRole.Button, new() { Name = "Export", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("invoices export");
        }
    }

    public sealed class SelectionToolbar(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_caption_the_selection_count_and_clear_it(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "selection-toolbar");
            await using var _ = context;
            var toolbar = page.Locator("#selection-toolbar");
            var caption = toolbar.Locator(".forma-selection-toolbar-caption");
            await page.Locator("#select").ClickAsync();
            await page.Locator("#select").ClickAsync();
            await Assertions.Expect(caption).ToHaveTextAsync("2 selected");

            // Act
            await toolbar.GetByRole(AriaRole.Button, new() { Name = "Clear Selection", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(caption).ToHaveTextAsync("0 selected");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("0");
        }
    }

    public sealed class StatusStrip(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_read_out_what_the_page_changes_and_run_its_command(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "status-strip");
            await using var _ = context;
            var strip = page.Locator("#status-strip");
            var readout = strip.GetByRole(AriaRole.Status);
            await Assertions.Expect(readout).ToHaveTextAsync("0 words");

            // Act
            await page.Locator("#add-word").ClickAsync();
            await page.Locator("#add-word").ClickAsync();
            await strip.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(readout).ToHaveTextAsync("2 words");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("save");
        }
    }

    public sealed class Toolbar(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_run_its_commands_and_move_focus_along_it_by_arrow_keys(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "toolbar");
            await using var _ = context;
            var toolbar = page.GetByRole(AriaRole.Toolbar, new() { Name = "Formatting", Exact = true });
            var bold = toolbar.GetByRole(AriaRole.Button, new() { Name = "Bold", Exact = true });
            var italic = toolbar.GetByRole(AriaRole.Button, new() { Name = "Italic", Exact = true });
            var underline = toolbar.GetByRole(AriaRole.Button, new() { Name = "Underline", Exact = true });

            // The toolbar takes the arrow keys once its script has taken every control but one out of the tab order, a script's
            // first answer, waited for as long as a page's own waits.
            await Assertions.Expect(italic).ToHaveAttributeAsync("tabindex", "-1", new() { Timeout = 30000 });

            // Act
            await bold.ClickAsync();
            await page.Keyboard.PressAsync("ArrowRight");
            await Assertions.Expect(italic).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("End");
            await Assertions.Expect(underline).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("bold underline");
            await Assertions.Expect(underline).ToBeFocusedAsync();
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_run_the_command_its_shortcut_names_in_its_target_until_it_is_hidden(string runtime)
        {
            // Arrange — Bold's shortcut run in the text area, then the toolbar's items swapped for Italic's
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "toolbar");
            await using var _ = context;
            var editor = page.Locator("#editor");
            var result = page.Locator("#shortcut-result");
            await ComponentChecks.WaitForInputRouteAsync(page, "#editor", "toolbar.bold");
            await editor.PressAsync("Control+b");
            await Assertions.Expect(result).ToHaveTextAsync("bold");
            await page.Locator("#swap").ClickAsync();
            await ComponentChecks.WaitForInputRouteAsync(page, "#editor", "toolbar.italic");

            // Act — Bold's shortcut pressed between two of Italic's, so the commands read in full only once every press was answered
            await editor.PressAsync("Control+i");
            await editor.PressAsync("Control+b");
            await editor.PressAsync("Control+i");
            await Assertions.Expect(result).ToHaveTextAsync("bold italic italic");
            await page.Locator("#hide").ClickAsync();
            await page.WaitForFunctionAsync("() => window.formaInputRouter.describe(document.querySelector('#editor')) === null");
            await editor.PressAsync("Control+i");
            await ComponentChecks.MakeRoundTripAsync(page);

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("bold italic italic");
        }
    }
}
