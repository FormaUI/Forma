namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The data view's main interactions through each of its views, in every runtime.</summary>
public sealed class DataViewTests
{
    public sealed class DataGrid(BrowserHost host)
    {
        private static readonly string[] SortedNames = ["Alpha", "Beta", "Gamma"];

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_sort_its_rows_by_the_header_pressed_and_select_the_row_pressed(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "data-grid");
            await using var _ = context;
            var header = page.GetByRole(AriaRole.Columnheader, new() { Name = "Name", Exact = true });
            var rows = page.Locator("#check tbody tr");
            await WaitForInputScopeAsync(page, "#check .forma-data-view-region");
            await Assertions.Expect(header).ToHaveAttributeAsync("aria-sort", "none");

            // Act — the rows settle in their sorted order before one is pressed
            await header.GetByRole(AriaRole.Button).ClickAsync();
            await Assertions.Expect(rows).ToContainTextAsync(SortedNames);
            await page.Locator("#check td", new() { HasText = "Beta" }).ClickAsync();

            // Assert
            await Assertions.Expect(header).ToHaveAttributeAsync("aria-sort", "ascending");
            await Assertions.Expect(rows).ToContainTextAsync(SortedNames);
            await Assertions.Expect(rows.Nth(1)).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(rows.Nth(0)).ToHaveAttributeAsync("aria-selected", "false");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Beta");
        }
    }

    public sealed class DataList(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_select_the_item_pressed(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "data-list");
            await using var _ = context;
            var item = page.Locator("#check [role=listitem]", new() { HasText = "Beta" });
            await WaitForInputScopeAsync(page, "#check .forma-data-view-region");

            // Act
            await item.Locator(".forma-data-list-item-name").ClickAsync();

            // Assert
            await Assertions.Expect(item).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(page.Locator("#check [role=listitem][aria-selected=true]")).ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Beta");
        }
    }

    public sealed class DataCards(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_select_the_card_pressed(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "data-cards");
            await using var _ = context;
            var card = page.Locator("#check [role=listitem]", new() { HasText = "Beta" });
            await WaitForInputScopeAsync(page, "#check .forma-data-view-region");

            // Act
            await card.Locator(".forma-data-card-name").ClickAsync();

            // Assert
            await Assertions.Expect(card).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(page.Locator("#check [role=listitem][aria-selected=true]")).ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Beta");
        }
    }

    public sealed class DataTree(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_the_node_whose_disclosure_is_pressed(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "data-tree");
            await using var _ = context;
            var folder = page.GetByRole(AriaRole.Treeitem, new() { Name = "Documents", Exact = true });
            var child = page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true });
            await Assertions.Expect(folder).ToHaveAttributeAsync("aria-expanded", "false");
            await Assertions.Expect(child).ToHaveCountAsync(0);

            // Act
            await page.GetByRole(AriaRole.Button, new() { Name = "Open Documents", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(folder).ToHaveAttributeAsync("aria-expanded", "true");
            await Assertions.Expect(child).ToBeVisibleAsync();
            await Assertions.Expect(child).ToHaveAttributeAsync("aria-level", "2");
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Close Documents", Exact = true })).ToBeVisibleAsync();
        }
    }
}
