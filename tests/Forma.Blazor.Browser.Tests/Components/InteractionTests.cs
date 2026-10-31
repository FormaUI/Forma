namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The input scope's, the host's routes' and the page title's interactions, in every runtime.</summary>
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

            // Act
            await page.Locator("#outside").PressAsync("ArrowDown");
            await inside.PressAsync("ArrowDown");
            await inside.PressAsync("ArrowUp");
            await inside.PressAsync("ArrowDown");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("check.next check.next");
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
}
