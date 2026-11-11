namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The explorer's interactions, in every runtime.</summary>
public sealed class ExplorerTests
{
    public sealed class Explorer(BrowserHost host)
    {
        private static readonly string[] RootItemNames = ["Covers", "Races", "cover.txt", "lap-times.txt"];

        private static readonly string[] RacesItemNames = ["lap-times.txt"];

        // Each card's own tab index, as its element carries it, and its box's.
        private const string TabIndexes = """
            () => [...document.querySelectorAll('#check .forma-data-card')]
                .map(card => `${card.getAttribute('tabindex')} ${card.querySelector('input[type=checkbox]')?.tabIndex}`)
                .join(', ')
            """;

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_list_every_item_and_only_the_open_folder_s_once_one_is_opened(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            var names = page.Locator("#check .forma-data-card-name");
            var sidebar = page.Locator("#check .forma-explorer-sidebar");
            var races = sidebar.Locator("[role=treeitem]", new() { HasText = "Races" });
            await Assertions.Expect(names).ToHaveTextAsync(RootItemNames);
            await Assertions.Expect(races).ToHaveAttributeAsync("aria-selected", "false");
            await ComponentChecks.WaitForInputRouteAsync(page, "#check .forma-data-tree", "tree.choose");

            // Act
            await races.Locator(".forma-data-tree-node-name").ClickAsync();

            // Assert
            await Assertions.Expect(names).ToHaveTextAsync(RacesItemNames);
            await Assertions.Expect(races).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(sidebar.Locator("[role=treeitem]", new() { HasText = "All Media" })).ToHaveAttributeAsync("aria-selected", "false");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Races");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_list_every_item_again_once_all_media_is_chosen_in_its_sidebar(string runtime)
        {
            // Arrange — Races opened from the sidebar
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            var names = page.Locator("#check .forma-data-card-name");
            var sidebar = page.Locator("#check .forma-explorer-sidebar");
            var allMedia = sidebar.Locator("[role=treeitem]", new() { HasText = "All Media" });
            await ComponentChecks.WaitForInputRouteAsync(page, "#check .forma-data-tree", "tree.choose");
            await sidebar.Locator("[role=treeitem]", new() { HasText = "Races" }).Locator(".forma-data-tree-node-name").ClickAsync();
            await Assertions.Expect(names).ToHaveTextAsync(RacesItemNames);

            // Act
            await allMedia.Locator(".forma-data-tree-node-name").ClickAsync();

            // Assert
            await Assertions.Expect(names).ToHaveTextAsync(RootItemNames);
            await Assertions.Expect(allMedia).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync(string.Empty);
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hand_the_host_a_file_dragged_onto_a_folder(string runtime)
        {
            // Arrange — the folder tree taking drops once its script routes them
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            await ComponentChecks.WaitForInputRouteAsync(page, "#check .forma-explorer-folder-tree", "explorer.drop");
            var file = page.Locator("#check .forma-data-card", new() { HasText = "cover.txt" });

            // Act
            await file.DragToAsync(page.Locator("#check .forma-explorer-sidebar [data-drop-key='races']"));

            // Assert
            await Assertions.Expect(page.Locator("#dropped")).ToHaveTextAsync("cover.txt to Races");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_a_focused_thumbnail_and_check_thumbnails_in_its_selection_mode(string runtime)
        {
            // Arrange — the thumbnails holding one tab stop, on the first
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            var cards = page.Locator("#check .forma-data-card");
            var boxes = cards.Locator("input[type=checkbox]");
            var select = page.Locator("#check .forma-explorer-header-wide").GetByRole(AriaRole.Button, new() { Name = "Select" });
            await Assertions.Expect(cards).ToHaveCountAsync(RootItemNames.Length);
            await ComponentChecks.WaitForInputScopeAsync(page, "#check .forma-data-view-region");
            await Assertions.Expect(cards.Nth(0)).ToHaveAttributeAsync("tabindex", "0");
            await Assertions.Expect(page.Locator("#check .forma-data-card[tabindex='0']")).ToHaveCountAsync(1);
            await cards.Nth(0).FocusAsync();

            // Act — the focus moved on to Races, which opens nothing and selects nothing
            await page.Keyboard.PressAsync("ArrowRight");
            await Assertions.Expect(cards.Nth(1)).ToBeFocusedAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync(string.Empty);
            await Assertions.Expect(page.Locator("#check .forma-data-card[aria-selected=true]")).ToHaveCountAsync(0);

            // Act — the selection mode on, the tab stop staying with Races on its box and the cards leaving the Tab sequence, then Covers
            // checked by a press and Races by Space
            await select.ClickAsync();
            await Assertions.Expect(boxes).ToHaveCountAsync(RootItemNames.Length);
            await page.WaitForFunctionAsync($"expected => ({TabIndexes})() === expected", "null -1, null 0, null -1, null -1");
            await cards.Nth(0).Locator(".forma-data-card-name").ClickAsync();
            await Assertions.Expect(boxes.Nth(0)).ToBeCheckedAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync(string.Empty);
            await page.Keyboard.PressAsync("ArrowRight");
            await Assertions.Expect(boxes.Nth(1)).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Space");
            await Assertions.Expect(boxes.Nth(1)).ToBeCheckedAsync();
            await page.Keyboard.PressAsync("Enter");

            // Assert — Enter opened Races
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Races");
            await Assertions.Expect(page.Locator("#check .forma-data-card-name")).ToHaveTextAsync(RacesItemNames);
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
            var status = page.Locator("#check .forma-data-view-status");

            // The explorer loads its next page while its end lies within 200px of the list's, and how many pages that takes
            // depends on when its tiles reach their height, so the list is read once its end lies beyond that, no page loading.
            await page.WaitForFunctionAsync(
                "() => { const scroll = document.querySelector('#check .forma-explorer-scroll'); const more = scroll?.querySelector('.forma-data-view-more'); return more !== null && more !== undefined && more.querySelector('[role=status], .forma-spinner') === null && more.getBoundingClientRect().top - scroll.getBoundingClientRect().bottom > 200; }",
                null,
                new() { PollingInterval = 100 });
            await Assertions.Expect(status).Not.ToHaveTextAsync("Showing 12 of 12");

            // Act — the list scrolled to its end each time a page arrives, until the last has
            await page.WaitForFunctionAsync(
                "() => { const scroll = document.querySelector('#check .forma-explorer-scroll'); scroll.scrollTop = scroll.scrollHeight; return document.querySelector('#check .forma-data-view-status')?.textContent.trim() === 'Showing 12 of 12'; }",
                null,
                new() { PollingInterval = 200 });

            // Assert
            await Assertions.Expect(status).ToHaveTextAsync("Showing 12 of 12");
            await Assertions.Expect(page.Locator("#check .forma-data-card-name")).ToHaveCountAsync(12);
        }
    }
}
