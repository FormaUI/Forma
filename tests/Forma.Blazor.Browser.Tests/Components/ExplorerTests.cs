namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The explorer's interactions, in every runtime.</summary>
public sealed class ExplorerTests
{
    public sealed class Explorer(BrowserHost host)
    {
        private static readonly string[] AllFileNames = ["cover.txt", "lap-times.txt"];

        private static readonly string[] RacesFileNames = ["lap-times.txt"];

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_list_every_item_and_only_the_open_folder_s_once_one_is_opened(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            var names = page.Locator("#check .forma-explorer-item-name");
            var sidebar = page.Locator("#check .forma-explorer-sidebar");
            var races = sidebar.Locator("[role=treeitem]", new() { HasText = "Races" });
            await Assertions.Expect(names).ToHaveTextAsync(AllFileNames);
            await Assertions.Expect(races).ToHaveAttributeAsync("aria-selected", "false");

            // Act
            await sidebar.GetByRole(AriaRole.Button, new() { Name = "Races", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(names).ToHaveTextAsync(RacesFileNames);
            await Assertions.Expect(races).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(sidebar.Locator("[role=treeitem]", new() { HasText = "All Media" })).ToHaveAttributeAsync("aria-selected", "false");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Races");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hand_the_host_a_file_dragged_onto_a_folder(string runtime)
        {
            // Arrange — the folder tree taking drops once its script routes them
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            await ComponentChecks.WaitForInputRouteAsync(page, "#check .forma-explorer-folder-tree", "explorer.drop");
            var file = page.Locator("#check .forma-explorer-item", new() { HasText = "cover.txt" });

            // Act
            await file.DragToAsync(page.Locator("#check [data-drop-key='races']"));

            // Assert
            await Assertions.Expect(page.Locator("#dropped")).ToHaveTextAsync("cover.txt to Races");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_fold_its_header_into_one_row_on_a_narrow_page_and_unfold_it_on_a_wide_one(string runtime)
        {
            // Arrange — the header laid out for a wide page
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            var viewport = page.ViewportSize!;
            var wide = page.Locator("#check .forma-explorer-header-wide");
            var compact = page.Locator("#check .forma-explorer-header-compact");
            await Assertions.Expect(compact).ToHaveCountAsync(0);
            await Assertions.Expect(wide).ToHaveCountAsync(1);

            // Act
            await page.SetViewportSizeAsync(360, viewport.Height);
            await Assertions.Expect(wide).ToHaveCountAsync(0);
            await Assertions.Expect(compact).ToHaveCountAsync(1);
            await page.SetViewportSizeAsync(viewport.Width, viewport.Height);

            // Assert
            await Assertions.Expect(compact).ToHaveCountAsync(0);
            await Assertions.Expect(wide).ToHaveCountAsync(1);
        }
    }

    public sealed class ExplorerScroll(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_append_the_next_page_as_it_scrolls_to_its_end(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer-scroll");
            await using var _ = context;
            var status = page.Locator("#check .forma-explorer-scroll-status");
            await Assertions.Expect(status).ToHaveTextAsync("Showing 4 of 12");

            // Act — the list scrolled to its end each time a page arrives, until the last has
            await page.WaitForFunctionAsync(
                "() => { const scroll = document.querySelector('#check .forma-explorer-scroll'); scroll.scrollTop = scroll.scrollHeight; return document.querySelector('#check .forma-explorer-scroll-status')?.textContent.trim() === 'Showing 12 of 12'; }",
                null,
                new() { PollingInterval = 200 });

            // Assert
            await Assertions.Expect(status).ToHaveTextAsync("Showing 12 of 12");
            await Assertions.Expect(page.Locator("#check .forma-explorer-item-name")).ToHaveCountAsync(12);
        }
    }
}
