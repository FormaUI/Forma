namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The collections' main interactions, in every runtime.</summary>
public sealed class CollectionsTests
{
    public sealed class SortableList(BrowserHost host)
    {
        private static readonly string[] ReorderedItems = ["Beta", "Alpha", "Gamma"];

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_move_its_first_item_down_by_its_handle_s_arrow_key(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "sortable-list");
            await using var _ = context;
            await WaitForInputScopeAsync(page, "#check");

            // Act
            await page.Locator("#check .forma-sortable-list-handle").First.PressAsync("ArrowDown");

            // Assert
            await Assertions.Expect(page.Locator("#check .forma-sortable-list-content")).ToHaveTextAsync(ReorderedItems);
            await Assertions.Expect(page.Locator(".forma-sortable-list-announcement")).ToContainTextAsync("Moved to position 2 of 3.");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Beta, Alpha, Gamma");
        }
    }
}
