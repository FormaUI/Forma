namespace Forma.Blazor.Browser.Tests.Components;

using System.Text.RegularExpressions;

/// <summary>The explorer's interactions, in every runtime.</summary>
public sealed class ExplorerTests
{
    public sealed class Explorer(BrowserHost host)
    {
        private static readonly string[] RootItemNames = ["Covers", "Races", "cover.txt", "lap-times.txt"];

        private static readonly string[] RacesItemNames = ["lap-times.txt"];

        private static readonly string[] CoversItemNames = ["cover.txt"];

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
        public async Task Should_review_a_picker_s_choices_from_two_folders_remove_one_and_choose_a_folder(string runtime)
        {
            // Arrange — in a picker of files and folders, a file chosen in Covers and one in Races, each entered from the sidebar
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            var picker = page.Locator("#picker");
            var sidebar = picker.Locator(".forma-explorer-sidebar");
            var names = picker.Locator(".forma-data-card-name");
            var chosen = page.Locator("#chosen");
            var review = picker.Locator(".forma-explorer-review-button");
            await Assertions.Expect(names).ToHaveTextAsync(RootItemNames);
            await ComponentChecks.WaitForInputRouteAsync(page, "#picker .forma-data-tree", "tree.choose");
            await ComponentChecks.WaitForInputScopeAsync(page, "#picker .forma-data-view-region");
            await sidebar.Locator("[role=treeitem]", new() { HasText = "Covers" }).Locator(".forma-data-tree-node-name").ClickAsync();
            await Assertions.Expect(names).ToHaveTextAsync(CoversItemNames);
            await names.GetByText("cover.txt", new() { Exact = true }).ClickAsync();
            await Assertions.Expect(chosen).ToHaveTextAsync("cover.txt");
            await sidebar.Locator("[role=treeitem]", new() { HasText = "Races" }).Locator(".forma-data-tree-node-name").ClickAsync();
            await Assertions.Expect(names).ToHaveTextAsync(RacesItemNames);
            await names.GetByText("lap-times.txt", new() { Exact = true }).ClickAsync();
            await Assertions.Expect(chosen).ToHaveTextAsync("cover.txt, lap-times.txt");

            // Act — the review opened on both, each with its folder, cover.txt removed from it, the review closed, and Races chosen
            await review.ClickAsync();
            var reviewed = picker.Locator(".forma-explorer-review-item");
            await Assertions.Expect(reviewed).ToHaveTextAsync([new Regex(@"cover\.txt\s*Covers"), new Regex(@"lap-times\.txt\s*Races")]);
            await picker.GetByRole(AriaRole.Button, new() { Name = "Remove cover.txt" }).ClickAsync();
            await Assertions.Expect(reviewed).ToHaveCountAsync(1);
            await Assertions.Expect(chosen).ToHaveTextAsync("lap-times.txt");
            await Assertions.Expect(review).ToHaveTextAsync("1 Selected");
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(review).ToBeFocusedAsync();
            await picker.GetByRole(AriaRole.Button, new() { Name = "Choose This Folder" }).ClickAsync();

            // Assert
            await Assertions.Expect(chosen).ToHaveTextAsync("Races, lap-times.txt");
            await Assertions.Expect(picker.Locator(".forma-explorer-choose-folder-button")).ToHaveAttributeAsync("aria-pressed", "true");
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

    public sealed class ExplorerKeyboard(BrowserHost host)
    {
        // The name of the content's item the focus stands on itself, where it stands on one.
        private const string FocusedItem = """
            () => {
                const focused = document.activeElement;
                const root = focused?.closest('#check .forma-explorer-content-backdrop [data-item-root]');
                return root && focused === root
                    ? root.querySelector('.forma-data-card-name, .forma-explorer-details-name-text, .forma-data-list-item-name')?.textContent?.trim() ?? null
                    : null;
            }
            """;

        // Whether the focus stands within one of the content's items.
        private const string IsWithinItems = "() => document.activeElement?.closest('#check .forma-explorer-content-backdrop [data-item-root]') != null";

        // Whether the selection behavior has gathered the controls of every one of the top level's four items but the one at the tab stop
        // out of the Tab sequence, which it does once a view's items are drawn.
        private const string IsGathered = """
            () => {
                const actions = [...document.querySelectorAll('#check .forma-explorer-content-backdrop [data-item-root] [data-item-actions]')];
                return actions.length === 4 && actions.filter(action => action.tabIndex >= 0).length === 1;
            }
            """;

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_enter_and_leave_the_thumbnails_at_one_tab_stop_and_run_an_item_s_actions_and_open_it_from_the_keyboard(string runtime)
            => CheckAsync(runtime, "Thumbnails", "ArrowRight");

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_enter_and_leave_the_details_at_one_tab_stop_and_run_an_item_s_actions_and_open_it_from_the_keyboard(string runtime)
            => CheckAsync(runtime, "Details", "ArrowDown");

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_enter_and_leave_the_list_at_one_tab_stop_and_run_an_item_s_actions_and_open_it_from_the_keyboard(string runtime)
            => CheckAsync(runtime, "List", "ArrowDown");

        // Tab from the sidebar's splitter enters the items at the first, Covers, then reaches its actions and leaves the items; back in, the
        // key moving on reaches Races, whose Rename, run from its actions opened with Shift+F10, asks for its new name and hands the focus
        // back to it once dismissed, and Enter then opens it.
        private async Task CheckAsync(string runtime, string view, string next)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            var items = page.Locator("#check .forma-explorer-content-backdrop [data-item-root]");
            await ComponentChecks.WaitForInputScopeAsync(page, "#check .forma-data-view-region");
            var viewButton = page.Locator("#check .forma-explorer-header-wide").GetByRole(AriaRole.Button, new() { Name = view, Exact = true });
            await viewButton.ClickAsync();
            await Assertions.Expect(viewButton).ToHaveAttributeAsync("aria-pressed", "true");
            await page.WaitForFunctionAsync(IsGathered);
            await page.Locator("#check .forma-explorer-splitter").FocusAsync();

            // Act — into the items past what the view draws before them, through the first item's actions and out
            var first = await TabIntoItemsAsync(page);
            await page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(items.Nth(0).Locator("[data-item-actions]")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            var isLeft = !await page.EvaluateAsync<bool>(IsWithinItems);

            // Act — back in, on to Races, its Rename run from its actions and dismissed
            await page.Keyboard.PressAsync("Shift+Tab");
            await Assertions.Expect(items.Nth(0).Locator("[data-item-actions]")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await WaitForFocusedItemAsync(page, "Covers");
            await page.Keyboard.PressAsync(next);
            await WaitForFocusedItemAsync(page, "Races");
            await page.Keyboard.PressAsync("Shift+F10");
            var menu = page.GetByRole(AriaRole.Menu);
            await ComponentChecks.WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = menu }));
            await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Rename", Exact = true }).PressAsync("Enter");
            var rename = page.Locator("dialog:has(#forma-explorer-rename-input)");
            await ComponentChecks.WaitForLayerAsync(rename);
            await Assertions.Expect(rename.Locator("#forma-explorer-rename-input")).ToHaveValueAsync("Races");
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(rename).ToHaveCountAsync(0);
            await WaitForFocusedItemAsync(page, "Races");
            await page.Keyboard.PressAsync("Enter");

            // Assert
            Assert.Equal("Covers", first);
            Assert.True(isLeft);
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Races");
        }

        // Presses Tab until the focus stands on an item, a view drawing a few controls of its own before them.
        private static async Task<string?> TabIntoItemsAsync(IPage page)
        {
            for (var press = 0; press < 12; press++)
            {
                await page.Keyboard.PressAsync("Tab");

                if (await page.EvaluateAsync<string?>(FocusedItem) is { } item)
                {
                    return item;
                }
            }

            return null;
        }

        private static async Task WaitForFocusedItemAsync(IPage page, string name)
            => await page.WaitForFunctionAsync($"name => ({FocusedItem})() === name", name);
    }

    public sealed class ExplorerDetails(BrowserHost host)
    {
        private static readonly string[] ByName = ["Archive", "alpha.txt", "beta.txt", "gamma.txt"];

        private static readonly string[] BySize = ["Archive", "beta.txt", "gamma.txt", "alpha.txt"];

        private static readonly string[] BySizeDescending = ["Archive", "alpha.txt", "gamma.txt", "beta.txt"];

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_sort_by_a_column_s_header_and_by_the_toolbar_s_menu_alike(string runtime)
        {
            // Arrange — the details sorted by name, the folder first
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer-details");
            await using var _ = context;
            var names = page.Locator("#check .forma-explorer-details-name-text");
            var nameHeader = page.Locator("#check th.forma-explorer-column-name");
            var sizeHeader = page.Locator("#check th.forma-explorer-column-size");
            var sortLabel = page.Locator("#check .forma-explorer-header-wide .forma-explorer-sort-trigger-text");
            await ComponentChecks.WaitForInputScopeAsync(page, "#check .forma-data-view-region");
            await Assertions.Expect(names).ToHaveTextAsync(ByName);
            await Assertions.Expect(nameHeader).ToHaveAttributeAsync("aria-sort", "ascending");

            // Act — the size header pressed, then pressed again
            await sizeHeader.GetByRole(AriaRole.Button).ClickAsync();
            await Assertions.Expect(names).ToHaveTextAsync(BySize);
            await Assertions.Expect(sizeHeader).ToHaveAttributeAsync("aria-sort", "ascending");
            await Assertions.Expect(nameHeader).ToHaveAttributeAsync("aria-sort", "none");
            await Assertions.Expect(sortLabel).ToHaveTextAsync("Size");
            await sizeHeader.GetByRole(AriaRole.Button).ClickAsync();
            await Assertions.Expect(names).ToHaveTextAsync(BySizeDescending);
            await Assertions.Expect(sizeHeader).ToHaveAttributeAsync("aria-sort", "descending");

            // Act — Name chosen from the toolbar's sort menu
            await page.Locator("#check .forma-explorer-header-wide .forma-explorer-sort-trigger").ClickAsync();
            await page.Locator("[data-sort-field='Name']").ClickAsync();

            // Assert — the header follows the menu
            await Assertions.Expect(names).ToHaveTextAsync(ByName);
            await Assertions.Expect(nameHeader).ToHaveAttributeAsync("aria-sort", "ascending");
            await Assertions.Expect(sizeHeader).ToHaveAttributeAsync("aria-sort", "none");
            await Assertions.Expect(sortLabel).ToHaveTextAsync("Name");
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
