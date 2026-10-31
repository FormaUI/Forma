namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The collections' interactions, in every runtime.</summary>
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
            await Assertions.Expect(page.Locator(".forma-sortable-list-container", new() { Has = page.Locator("#check") }).Locator(".forma-sortable-list-announcement")).ToContainTextAsync("Moved to position 2 of 3.");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Beta, Alpha, Gamma");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_move_its_first_item_below_the_second_by_a_drag_of_its_handle(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "sortable-list");
            await using var _ = context;
            await WaitForInputScopeAsync(page, "#check");
            var items = page.Locator("#check .forma-sortable-list-item");

            // Act
            await DragAsync(page, page.Locator("#check .forma-sortable-list-handle").First, items.Nth(1));

            // Assert
            await Assertions.Expect(page.Locator("#check .forma-sortable-list-content")).ToHaveTextAsync(ReorderedItems);
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Beta, Alpha, Gamma");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_move_a_row_dragged_by_its_content_where_its_rows_are_dragged_whole(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "sortable-list");
            await using var _ = context;
            await WaitForInputScopeAsync(page, "#rows");
            var rows = page.Locator("#rows .forma-sortable-list-item");

            // Act
            await DragAsync(page, page.Locator("#rows .forma-sortable-list-content").First, rows.Nth(1));

            // Assert
            await Assertions.Expect(page.Locator("#rows-result")).ToHaveTextAsync("Green, Red, Blue");
        }

        // A drag from the middle of what is held to the lower half of the item it lands after.
        private static async Task DragAsync(IPage page, ILocator held, ILocator target)
        {
            var from = (await held.BoundingBoxAsync())!;
            var to = (await target.BoundingBoxAsync())!;

            await page.Mouse.MoveAsync(from.X + (from.Width / 2), from.Y + (from.Height / 2));
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(to.X + (to.Width / 2), to.Y + (to.Height * 0.75f), new() { Steps = 8 });
            await page.Mouse.UpAsync();
        }
    }
}
