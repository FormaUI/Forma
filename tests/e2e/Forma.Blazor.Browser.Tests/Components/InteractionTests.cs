namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The input scope's, the host's routes', the page title's and the page head's interactions, each in every runtime that renders it.</summary>
public sealed class InteractionTests
{
    public sealed class InputScope(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_route_the_key_pressed_inside_it_to_its_action(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "input-scope");
            await using var _ = context;
            var inside = page.Locator("#inside");
            await WaitForInputScopeAsync(page, "#scope");

            // Act — Enter last, an action no earlier press makes, so the actions read in full only once every press was answered
            await page.Locator("#outside").PressAsync("ArrowDown");
            await inside.PressAsync("ArrowDown");
            await inside.PressAsync("ArrowUp");
            await inside.PressAsync("ArrowDown");
            await inside.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("check.next check.next check.end");
        }
    }

    public sealed class HostRoute(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_perform_the_host_s_own_shortcut_through_the_host_s_handler(string runtime)
        {
            // Arrange — the host's routes configured, as the first scope on the page does
            var (context, page) = await OpenAsync(host, runtime, "host-route");
            await using var _ = context;
            await WaitForInputScopeAsync(page, "#scope");
            await page.Locator("#inside").FocusAsync();

            // Act
            await page.Keyboard.PressAsync("F2");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("check.host");
        }
    }

    public sealed class PageTitle(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_head_the_page_with_the_title_its_action_renames(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "page-title");
            await using var _ = context;
            var pageTitle = page.Locator("#page-title");
            var heading = pageTitle.GetByRole(AriaRole.Heading, new() { Level = 1 });
            await Assertions.Expect(heading).ToHaveTextAsync("Orders");
            await Assertions.Expect(pageTitle.GetByRole(AriaRole.Link, new() { Name = "Store", Exact = true })).ToBeVisibleAsync();

            // Act
            await pageTitle.GetByRole(AriaRole.Button, new() { Name = "Rename", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(heading).ToHaveTextAsync("Invoices");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Invoices");
        }
    }

    public sealed class PageHead(BrowserHost host)
    {
        // The title the components host's head gives a page that sets none.
        private const string HostTitle = "Forma Component Checks";

        /// <summary>The runtimes a Web App renders a page in: static rendering, the Server, WebAssembly, and Auto on the Server and on WebAssembly.</summary>
        public static TheoryData<string> WebAppRuntimes { get; } = [.. ((string[])["static", "server", "webassembly", "auto-server", "auto-webassembly"]).Select(runtime => new TheoryDataRow<string>(runtime).WithTrait("Runtime", runtime))];

        /// <summary>The runtimes a Web App renders a page in interactively.</summary>
        public static TheoryData<string> InteractiveRuntimes { get; } = [.. ((string[])["server", "webassembly", "auto-server", "auto-webassembly"]).Select(runtime => new TheoryDataRow<string>(runtime).WithTrait("Runtime", runtime))];

        [Theory]
        [MemberData(nameof(WebAppRuntimes))]
        public async Task Should_title_the_document_with_the_page_s_own_title_alone_and_with_the_host_s_where_a_page_sets_none(string runtime)
        {
            // Arrange — a page that sets its own title
            var (context, page) = await OpenInWebAppAsync(host, runtime, "page-title");
            await using var _ = context;
            var titles = page.Locator("title");
            await Assertions.Expect(page).ToHaveTitleAsync($"Orders · {HostTitle}");
            await Assertions.Expect(titles).ToHaveCountAsync(1);

            // Act — a navigation to a page that sets none
            await page.EvaluateAsync("path => Blazor.navigateTo(path)", $"{SegmentOf(runtime)}/field");
            await Assertions.Expect(page.Locator("#name")).ToBeVisibleAsync();

            // Assert
            await Assertions.Expect(page).ToHaveTitleAsync(HostTitle);
            await Assertions.Expect(titles).ToHaveCountAsync(1);
        }

        [Theory]
        [MemberData(nameof(InteractiveRuntimes))]
        public async Task Should_title_the_document_with_the_page_s_title_as_the_page_changes_it(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "page-title");
            await using var _ = context;
            await Assertions.Expect(page).ToHaveTitleAsync($"Orders · {HostTitle}");

            // Act
            await page.Locator("#rename").ClickAsync();

            // Assert
            await Assertions.Expect(page).ToHaveTitleAsync($"Invoices · {HostTitle}");
            await Assertions.Expect(page.Locator("title")).ToHaveCountAsync(1);
        }

        // Opens the check page at the path in the runtime; under static rendering, the page has no runtime to wait for.
        private static async Task<(IBrowserContext Context, IPage Page)> OpenInWebAppAsync(BrowserHost host, string runtime, string path)
        {
            if (runtime != "static")
            {
                return await OpenAsync(host, runtime, path);
            }

            var (context, page) = await host.OpenPageAsync(await host.GetComponentsAddressAsync());
            await page.GotoAsync($"static/{path}");

            return (context, page);
        }

        // The address segment that names the runtime, which Auto's two runtimes share.
        private static string SegmentOf(string runtime)
            => runtime.StartsWith("auto-", StringComparison.Ordinal) ? "auto" : runtime;
    }
}
