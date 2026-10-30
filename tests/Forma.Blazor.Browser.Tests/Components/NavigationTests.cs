namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The navigation components' main interactions, in every runtime.</summary>
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
    }
}
