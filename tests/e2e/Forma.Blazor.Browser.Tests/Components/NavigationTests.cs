namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The navigation components' interactions, in every runtime.</summary>
public sealed class NavigationTests
{
    public sealed class Breadcrumb(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_report_the_segment_pressed(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "breadcrumb");
            await using var _ = context;
            var breadcrumb = page.Locator("#breadcrumb");

            // Act
            await breadcrumb.GetByRole(AriaRole.Button, new() { Name = "Store", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("store");
            await Assertions.Expect(breadcrumb.Locator("[aria-current=page]")).ToHaveTextAsync("Orders");
        }
    }

    public sealed class Pagination(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_move_to_the_page_pressed_and_a_run_of_pages_on_next(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "pagination");
            await using var _ = context;
            var steps = page.Locator("#pagination .forma-pagination-navigation-steps");
            var result = page.Locator("#result");
            await Assertions.Expect(result).ToHaveTextAsync("1");

            // Act
            await steps.GetByRole(AriaRole.Button, new() { Name = "Page 3", Exact = true }).ClickAsync();
            await Assertions.Expect(result).ToHaveTextAsync("3");
            await steps.Locator(".forma-pagination-navigation-button-next").ClickAsync();

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("7");
            await Assertions.Expect(steps.Locator("[aria-current=page]")).ToHaveTextAsync("7");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_page_forward_and_back_as_the_wheel_turns_over_it(string runtime)
        {
            // Arrange — the pointer over the pagination, whose wheel script routes a turn as a page
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "pagination");
            await using var _ = context;
            var result = page.Locator("#result");
            await ComponentChecks.WaitForInputRouteAsync(page, "#pagination", "pagination.page");
            await page.Locator("#pagination").HoverAsync();

            // Act
            await page.Mouse.WheelAsync(0, 100);
            await Assertions.Expect(result).ToHaveTextAsync("2");
            await page.Mouse.WheelAsync(0, -100);

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("1");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_select_its_entry_on_focus_and_go_to_the_page_typed_over_it(string runtime)
        {
            // Arrange — the entry focused, its whole text selected so the first key replaces it
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "pagination");
            await using var _ = context;
            var entry = page.Locator("#entry .forma-pagination-navigation-input-box");
            await ComponentChecks.WaitForInputRouteAsync(page, "#entry .forma-pagination-navigation-input-box", "pagination.go");
            await entry.FocusAsync();
            await page.WaitForFunctionAsync("entry => entry.value.length > 0 && entry.selectionStart === 0 && entry.selectionEnd === entry.value.length", await entry.ElementHandleAsync());

            // Act
            await entry.PressSequentiallyAsync("7");
            await entry.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#entry-result")).ToHaveTextAsync("7");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("1");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_go_to_a_page_its_entry_holds_though_no_input_reported_it(string runtime)
        {
            // Arrange — the entry emptied, then given a page with no input event, as text typed before the runtime listens is
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "pagination");
            await using var _ = context;
            var entry = page.Locator("#entry .forma-pagination-navigation-input-box");
            await ComponentChecks.WaitForInputRouteAsync(page, "#entry .forma-pagination-navigation-input-box", "pagination.go");
            await entry.FillAsync(string.Empty);
            await entry.EvaluateAsync("entry => entry.value = '9'");

            // Act
            await entry.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#entry-result")).ToHaveTextAsync("9");
        }
    }
}
