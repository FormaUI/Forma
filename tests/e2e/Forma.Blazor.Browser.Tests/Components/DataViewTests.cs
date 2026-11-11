namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The data view's interactions through each of its views, in every runtime.</summary>
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hold_the_height_of_a_full_page_on_a_last_page_that_holds_fewer_rows(string runtime)
        {
            // Arrange — the first page of three rows, its height reserved for the pages after it
            var (context, page) = await OpenAsync(host, runtime, "data-grid-pages");
            await using var _ = context;
            var region = page.Locator("#check .forma-data-view-region");
            await Assertions.Expect(page.Locator("#check tbody tr")).ToHaveCountAsync(3);
            await page.WaitForFunctionAsync("region => region.style.minBlockSize !== ''", await region.ElementHandleAsync());
            var reserved = await region.EvaluateAsync<string>("region => region.style.minBlockSize");
            var height = (await region.BoundingBoxAsync())!.Height;

            // Act
            await page.Locator("#check").GetByRole(AriaRole.Button, new() { Name = "Page 2", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#check tbody tr")).ToHaveCountAsync(2);
            Assert.Equal(reserved, await region.EvaluateAsync<string>("region => region.style.minBlockSize"));
            Assert.Equal(height, (await region.BoundingBoxAsync())!.Height, 1.0);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_drop_the_columns_a_narrow_width_has_no_room_for_and_bring_them_back_as_it_widens(string runtime)
        {
            // Arrange — every column shown on a wide page
            var (context, page) = await OpenAsync(host, runtime, "data-grid-fit");
            await using var _ = context;
            var viewport = page.ViewportSize!;
            var notes = page.GetByRole(AriaRole.Columnheader, new() { Name = "Notes", Exact = true });
            var count = page.GetByRole(AriaRole.Columnheader, new() { Name = "Count", Exact = true });
            await Assertions.Expect(notes).ToBeVisibleAsync();
            await Assertions.Expect(count).ToBeVisibleAsync();

            // Act — each width the grid's script reports, which the check waits for as it waits for a script's answer
            await page.SetViewportSizeAsync(360, viewport.Height);
            await Assertions.Expect(notes).ToHaveCountAsync(0, new() { Timeout = 30_000 });
            await page.SetViewportSizeAsync(viewport.Width, viewport.Height);

            // Assert
            await Assertions.Expect(notes).ToBeVisibleAsync(new() { Timeout = 30_000 });
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Name", Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(count).ToBeVisibleAsync();
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_add_the_item_pressed_with_control_and_take_in_those_between_on_a_press_with_shift(string runtime)
        {
            // Arrange — Alpha pressed, then Gamma added to it
            var (context, page) = await OpenAsync(host, runtime, "data-list");
            await using var _ = context;
            var result = page.Locator("#result");
            await WaitForInputScopeAsync(page, "#check .forma-data-view-region");
            await GetName(page, "Alpha").ClickAsync();
            await GetName(page, "Gamma").ClickAsync(new() { Modifiers = [KeyboardModifier.ControlOrMeta] });
            await Assertions.Expect(page.Locator("#check [role=listitem][aria-selected=true]")).ToHaveCountAsync(2);
            await Assertions.Expect(result).ToHaveTextAsync("Alpha, Gamma");
            await GetName(page, "Alpha").ClickAsync();

            // Act
            await GetName(page, "Gamma").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

            // Assert
            await Assertions.Expect(page.Locator("#check [role=listitem][aria-selected=true]")).ToHaveCountAsync(3);
            await Assertions.Expect(result).ToHaveTextAsync("Alpha, Beta, Gamma");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_select_the_item_the_arrow_keys_reach_and_activate_it_on_enter(string runtime)
        {
            // Arrange — Alpha pressed, which focuses its marker; the items stand side by side on a wide page
            var (context, page) = await OpenAsync(host, runtime, "data-list");
            await using var _ = context;
            await WaitForInputScopeAsync(page, "#check .forma-data-view-region");
            await GetName(page, "Alpha").ClickAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Alpha");

            // Act
            await page.Keyboard.PressAsync("ArrowRight");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Beta");
            await page.Keyboard.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#check [role=listitem]", new() { HasText = "Beta" }).Locator("[data-row-key]")).ToBeFocusedAsync();
            await Assertions.Expect(page.Locator("#activated")).ToHaveTextAsync("Beta");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_select_by_position_and_seek_to_either_end_of_items_it_has_not_drawn(string runtime)
        {
            // Arrange — the first of two hundred items pressed, then two more taken in by Shift with the arrow
            var (context, page) = await OpenAsync(host, runtime, "data-list-long");
            await using var _ = context;
            var count = page.Locator("#count");
            await WaitForInputScopeAsync(page, "#check .forma-data-view-region");
            await GetName(page, "Item 0").ClickAsync();
            await page.Keyboard.PressAsync("Shift+ArrowDown");
            await page.Keyboard.PressAsync("Shift+ArrowDown");
            await Assertions.Expect(count).ToHaveTextAsync("3");

            // Act — to the last item, far past those drawn, then back to the first with Shift
            await page.Keyboard.PressAsync("End");
            await Assertions.Expect(page.Locator("#check [role=listitem]", new() { Has = GetNameWithin(page, "Item 199") }).Locator("[data-row-key]")).ToBeFocusedAsync();
            await Assertions.Expect(count).ToHaveTextAsync("1");
            await page.Keyboard.PressAsync("Shift+Home");

            // Assert
            await Assertions.Expect(count).ToHaveTextAsync("200");
            await Assertions.Expect(page.Locator("#check [role=listitem]", new() { Has = GetNameWithin(page, "Item 0") }).Locator("[data-row-key]")).ToBeFocusedAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_switch_to_the_view_its_switch_names(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "data-list-long");
            await using var _ = context;
            await WaitForInputScopeAsync(page, "#check .forma-data-view-region");
            var grid = page.Locator("#switch").GetByRole(AriaRole.Button, new() { Name = "Grid", Exact = true });

            // Act
            await grid.ClickAsync();

            // Assert
            await Assertions.Expect(grid).ToHaveAttributeAsync("aria-pressed", "true");
            await Assertions.Expect(page.Locator("#check table")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Name", Exact = true })).ToBeVisibleAsync();
        }

        private static ILocator GetName(IPage page, string name)
            => page.Locator("#check").Locator(GetNameWithin(page, name));

        // The name as an item holds it, which a locator filtering the items looks for within each.
        private static ILocator GetNameWithin(IPage page, string name)
            => page.Locator(".forma-data-list-item-name", new() { HasTextRegex = new System.Text.RegularExpressions.Regex($"^{name}$") });
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_lay_as_many_cards_to_a_row_as_its_width_holds(string runtime)
        {
            // Arrange — the cards laid out on a wide page, once their columns are measured
            var (context, page) = await OpenAsync(host, runtime, "data-cards-long");
            await using var _ = context;
            var firstRow = page.Locator("#check .forma-data-cards-row").First;
            await Assertions.Expect(firstRow.Locator("[role=listitem]").First).ToBeVisibleAsync();
            var wide = await firstRow.Locator("[role=listitem]").CountAsync();
            Assert.True(wide > 2);

            // Act
            await page.SetViewportSizeAsync(360, page.ViewportSize!.Height);

            // Assert
            await page.WaitForFunctionAsync("count => document.querySelector('#check .forma-data-cards-row').querySelectorAll('[role=listitem]').length < count", wide);
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_move_through_and_activate_its_nodes_by_the_keys_and_the_first_letters_typed(string runtime)
        {
            // Arrange — the folder focused once the tree's keys are routed
            var (context, page) = await OpenAsync(host, runtime, "data-tree");
            await using var _ = context;
            var folder = page.GetByRole(AriaRole.Treeitem, new() { Name = "Documents", Exact = true });
            await WaitForInputScopeAsync(page, "#check .forma-data-tree");
            await folder.FocusAsync();

            // Act
            await page.Keyboard.PressAsync("ArrowRight");
            await Assertions.Expect(folder).ToHaveAttributeAsync("aria-expanded", "true");
            await page.Keyboard.PressAsync("ArrowDown");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true })).ToBeFocusedAsync();
            await page.Keyboard.TypeAsync("c");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Contracts", Exact = true })).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("End");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Readme.txt", Exact = true })).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#activated")).ToHaveTextAsync("Readme.txt");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hand_the_focus_to_the_folder_whose_node_closes_over_it(string runtime)
        {
            // Arrange — a node inside the open folder focused
            var (context, page) = await OpenAsync(host, runtime, "data-tree");
            await using var _ = context;
            var folder = page.GetByRole(AriaRole.Treeitem, new() { Name = "Documents", Exact = true });
            await page.GetByRole(AriaRole.Button, new() { Name = "Open Documents", Exact = true }).ClickAsync();
            var reports = page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true });
            await reports.FocusAsync();

            // Act — a press that leaves the focus where it was, as a pointer press on the disclosure does
            await page.GetByRole(AriaRole.Button, new() { Name = "Close Documents", Exact = true }).DispatchEventAsync("click");

            // Assert
            await Assertions.Expect(reports).ToHaveCountAsync(0);
            await Assertions.Expect(folder).ToBeFocusedAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_take_tab_first_to_its_first_node_and_back_to_the_node_last_focused_in_it(string runtime)
        {
            // Arrange — the tree watching where the focus is
            var (context, page) = await OpenAsync(host, runtime, "data-tree");
            await using var _ = context;
            var after = page.Locator("#after");
            var reports = page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true });
            await WaitForObservationAsync(page, "#check .forma-data-tree", "focus");

            // Act — entered from the button after it
            await after.FocusAsync();
            await page.Keyboard.PressAsync("Shift+Tab");

            // Assert
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Documents", Exact = true })).ToBeFocusedAsync();

            // Act — Reports focused, the focus taken out of the tree, and the tree entered again
            await page.GetByRole(AriaRole.Button, new() { Name = "Open Documents", Exact = true }).ClickAsync();
            await reports.FocusAsync();
            await Assertions.Expect(reports).ToHaveAttributeAsync("tabindex", "0", new() { Timeout = 30000 });
            await after.FocusAsync();
            await page.Keyboard.PressAsync("Shift+Tab");

            // Assert
            await Assertions.Expect(reports).ToBeFocusedAsync();
            await Assertions.Expect(page.Locator("#activated")).ToBeEmptyAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_leave_the_page_s_focus_where_it_is_and_take_tab_to_the_node_in_place_of_one_removed_while_the_focus_was_elsewhere(string runtime)
        {
            // Arrange — Contracts focused inside the open Documents
            var (context, page) = await OpenAsync(host, runtime, "data-tree");
            await using var _ = context;
            var remove = page.Locator("#remove-contracts");
            var contracts = page.GetByRole(AriaRole.Treeitem, new() { Name = "Contracts", Exact = true });
            await WaitForObservationAsync(page, "#check .forma-data-tree", "focus");
            await page.GetByRole(AriaRole.Button, new() { Name = "Open Documents", Exact = true }).ClickAsync();
            await contracts.FocusAsync();
            await Assertions.Expect(contracts).ToHaveAttributeAsync("tabindex", "0", new() { Timeout = 30000 });

            // Act — Contracts taken away by a button outside the tree, which holds the focus, pressed from the keyboard since a pointer's
            // press focuses a button in Chromium alone
            await remove.FocusAsync();
            await remove.PressAsync("Enter");
            await Assertions.Expect(contracts).ToHaveCountAsync(0);

            // Assert — the page's focus where it was, and Tab back into the tree at Reports, the sibling before Contracts
            await Assertions.Expect(remove).ToBeFocusedAsync();
            await page.Locator("#after").FocusAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true })).ToBeFocusedAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_leave_the_page_s_focus_where_it_is_and_take_tab_to_the_folder_closed_over_the_node_last_focused_while_the_focus_was_elsewhere(string runtime)
        {
            // Arrange — Reports focused inside the open Documents, then the focus taken out of the tree
            var (context, page) = await OpenAsync(host, runtime, "data-tree");
            await using var _ = context;
            var after = page.Locator("#after");
            var reports = page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true });
            await WaitForObservationAsync(page, "#check .forma-data-tree", "focus");
            await page.GetByRole(AriaRole.Button, new() { Name = "Open Documents", Exact = true }).ClickAsync();
            await reports.FocusAsync();
            await Assertions.Expect(reports).ToHaveAttributeAsync("tabindex", "0", new() { Timeout = 30000 });
            await after.FocusAsync();

            // Act — Documents closed by a press that leaves the focus where it is, as a pointer press on the disclosure does
            await page.GetByRole(AriaRole.Button, new() { Name = "Close Documents", Exact = true }).DispatchEventAsync("click");
            await Assertions.Expect(reports).ToHaveCountAsync(0);

            // Assert — the page's focus where it was, and Tab back into the tree at Documents
            await Assertions.Expect(after).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Documents", Exact = true })).ToBeFocusedAsync();
        }
    }

    public sealed class DataTreeSidebar(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_take_tab_back_to_its_current_node_whatever_was_focused_in_it(string runtime)
        {
            // Arrange — Readme.txt focused without being chosen, then the focus taken out of the sidebar
            var (context, page) = await OpenAsync(host, runtime, "data-tree-sidebar");
            await using var _ = context;
            var after = page.Locator("#after");
            await WaitForObservationAsync(page, "#check .forma-data-tree", "focus");
            await page.GetByRole(AriaRole.Treeitem, new() { Name = "Readme.txt", Exact = true }).FocusAsync();
            await MakeRoundTripAsync(page);
            await after.FocusAsync();

            // Act
            await page.Keyboard.PressAsync("Shift+Tab");

            // Assert — Documents, still the current node
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Documents", Exact = true })).ToBeFocusedAsync();
            await Assertions.Expect(page.Locator("#current")).ToHaveTextAsync("Documents");
        }
    }

    public sealed class DataTreeOpen(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_the_named_folder_on_load_over_the_current_node_and_keep_it_closed_through_a_refresh(string runtime)
        {
            // Arrange — Documents named open, and Reports, inside it, current
            var (context, page) = await OpenAsync(host, runtime, "data-tree-open");
            await using var _ = context;
            var documents = page.GetByRole(AriaRole.Treeitem, new() { Name = "Documents", Exact = true });
            await Assertions.Expect(documents).ToHaveAttributeAsync("aria-expanded", "true");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true })).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Pictures", Exact = true })).ToHaveAttributeAsync("aria-expanded", "false");

            // Act
            await page.GetByRole(AriaRole.Button, new() { Name = "Close Documents", Exact = true }).ClickAsync();
            await Assertions.Expect(documents).ToHaveAttributeAsync("aria-expanded", "false");
            await page.Locator("#refresh").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#refreshes")).ToHaveTextAsync("1");
            await Assertions.Expect(documents).ToHaveAttributeAsync("aria-expanded", "false");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true })).ToHaveCountAsync(0);
        }
    }

    public sealed class DataTreeHost(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_keep_the_current_folder_the_open_folders_the_selection_and_the_view_as_the_host_resizes_and_stacks_its_sidebar(string runtime)
        {
            // Arrange — Documents opened and Reports chosen in the sidebar, the list shown and Q1.pdf selected in it
            var (context, page) = await OpenAsync(host, runtime, "data-tree-host");
            await using var _ = context;
            await WaitForInputScopeAsync(page, "#check .forma-data-view-region");
            await WaitForInputRouteAsync(page, "#check .forma-data-tree", "tree.choose");
            await page.GetByRole(AriaRole.Button, new() { Name = "Open Documents", Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true }).Locator(".forma-data-tree-node-name").ClickAsync();
            await Assertions.Expect(page.Locator("#current")).ToHaveTextAsync("Reports");
            await page.Locator("#switch").GetByRole(AriaRole.Button, new() { Name = "List", Exact = true }).ClickAsync();
            await page.Locator("#check [role=listitem]", new() { HasText = "Q1.pdf" }).Locator(".forma-data-list-item-name").ClickAsync();
            await Assertions.Expect(page.Locator("#footer .forma-data-view-status")).ToHaveTextAsync("1 selected");

            // Act — the sidebar widened from the keyboard, then stacked above the content
            await WaitForInputScopeAsync(page, "#splitter");
            await page.Locator("#splitter").PressAsync("ArrowRight");
            await Assertions.Expect(page.Locator("#width")).ToHaveTextAsync("13");
            await page.Locator("#stack").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#arrangement")).ToHaveCSSAsync("flex-direction", "column");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Documents", Exact = true })).ToHaveAttributeAsync("aria-expanded", "true");
            await Assertions.Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Reports", Exact = true })).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(page.Locator("#switch").GetByRole(AriaRole.Button, new() { Name = "List", Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");
            await Assertions.Expect(page.Locator("#check [role=listitem]", new() { HasText = "Q1.pdf" })).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(page.Locator("#content > #footer .forma-data-view-status")).ToHaveTextAsync("1 selected");
        }
    }

    public sealed class DataViewTabOrder(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_leave_two_hundred_items_after_one_item_s_controls_and_run_an_item_s_commands_from_the_keyboard_in_the_list(string runtime)
            => CheckAsync(runtime, "list", "ArrowDown");

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_leave_two_hundred_items_after_one_item_s_controls_and_run_an_item_s_commands_from_the_keyboard_in_the_cards(string runtime)
            => CheckAsync(runtime, "cards", "ArrowRight");

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_leave_two_hundred_items_after_one_item_s_controls_and_run_an_item_s_commands_from_the_keyboard_in_the_grid(string runtime)
            => CheckAsync(runtime, "grid", "ArrowDown");

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_leave_two_hundred_items_after_one_item_s_controls_and_run_an_item_s_commands_from_the_keyboard_in_the_tree(string runtime)
            => CheckAsync(runtime, "tree", "ArrowDown");

        // Tab enters the view's items at the first and leaves the view after that item's Open button and its actions; the key moving on reaches
        // the second item, whose Open button Tab then reaches and Enter runs, and Shift+F10 on it opens its actions, whose Rename runs
        // and hands the focus back to the item.
        private async Task CheckAsync(string runtime, string view, string next)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "data-view-tab-order");
            await using var _ = context;
            var items = page.Locator($"#{view} [data-item-root]");
            await Assertions.Expect(items).ToHaveCountAsync(200);
            await WaitForInputScopeAsync(page, $"#{view} .forma-data-view-region");

            if (view == "tree")
            {
                await WaitForInputRouteAsync(page, "#tree .forma-data-tree", "tree.next");
            }

            await page.Locator($"#before-{view}").FocusAsync();

            // Act — into the view past the controls a view draws before its items, a grid's header, then through the first item's and out
            var first = await TabIntoItemsAsync(page);
            await page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(items.Nth(0).GetByRole(AriaRole.Button, new() { Name = "Open Item 1", Exact = true })).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(items.Nth(0).GetByRole(AriaRole.Button, new() { Name = "Actions for Item 1", Exact = true })).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(page.Locator($"#after-{view}")).ToBeFocusedAsync();

            // Act — back in, on to the second item, its Open button run from the keyboard
            await page.Keyboard.PressAsync("Shift+Tab");
            await page.Keyboard.PressAsync("Shift+Tab");
            await page.Keyboard.PressAsync("Shift+Tab");
            await page.Keyboard.PressAsync(next);
            await WaitForFocusedItemAsync(page, "Item 2");
            await page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(items.Nth(1).GetByRole(AriaRole.Button, new() { Name = "Open Item 2", Exact = true })).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Opened Item 2");

            // Act — the second item's actions opened with Shift+F10 and its Rename run
            await page.Keyboard.PressAsync("Shift+Tab");
            await page.Keyboard.PressAsync("Shift+F10");
            var menu = page.GetByRole(AriaRole.Menu);
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = menu }));
            await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Rename", Exact = true }).PressAsync("Enter");

            // Assert
            Assert.Equal("Item 1", first);
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Renamed Item 2");
            await WaitForFocusedItemAsync(page, "Item 2");
        }

        // The name of the item the focus stands on, by its marker or itself, where the focus stands on neither an item's control nor outside.
        private const string FocusedItem = """
            () => {
                const focused = document.activeElement;
                const root = focused?.closest('[data-item-root]');
                const isStop = focused === root || focused?.matches('[data-row-key]');
                return root && isStop ? root.querySelector('.forma-data-list-item-name, .forma-data-card-name, .forma-data-tree-node-name, td:nth-child(2)')?.textContent?.trim() ?? null : null;
            }
            """;

        private static Task<string?> FocusedItemAsync(IPage page)
            => page.EvaluateAsync<string?>(FocusedItem);

        // Presses Tab until the focus stands on an item, a view drawing a few controls of its own before them.
        private static async Task<string?> TabIntoItemsAsync(IPage page)
        {
            for (var press = 0; press < 4; press++)
            {
                await page.Keyboard.PressAsync("Tab");

                if (await FocusedItemAsync(page) is { } item)
                {
                    return item;
                }
            }

            return null;
        }

        private static async Task WaitForFocusedItemAsync(IPage page, string name)
            => await page.WaitForFunctionAsync($"name => ({FocusedItem})() === name", name);
    }

    public sealed class DataViewItemFocus(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_focus_open_and_check_the_list_s_items_apart_from_their_selection(string runtime)
            => CheckAsync(runtime, "list", "ArrowDown");

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_focus_open_and_check_the_cards_apart_from_their_selection(string runtime)
            => CheckAsync(runtime, "cards", "ArrowRight");

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public Task Should_focus_open_and_check_the_grid_s_rows_apart_from_their_selection(string runtime)
            => CheckAsync(runtime, "grid", "ArrowDown");

        // The name of the item the focus stands on, by its own element or its box, where the focus stands on neither a control nor outside.
        private const string FocusedItem = """
            () => {
                const focused = document.activeElement;
                const root = focused?.closest('[data-item-root]');
                return root && (focused === root || focused.matches('[data-row-key]')) ? root.getAttribute('data-name') : null;
            }
            """;

        // With no selection on, Tab enters at the first item and leaves after it, the key moving on focuses the next alone, and Enter and a
        // press open; the selection mode's switch keeps the focus on its item, a key moves among the boxes, Space checks one, Enter still
        // opens and a press checks; and the switch back hands the focus to the item again.
        private async Task CheckAsync(string runtime, string view, string next)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "data-view-item-focus");
            await using var _ = context;
            var items = page.Locator($"#{view} [data-item-root]");
            var opened = page.Locator($"#opened-{view}");
            var selected = page.Locator($"#selected-{view}");
            await Assertions.Expect(items).ToHaveCountAsync(20);
            await WaitForInputScopeAsync(page, $"#{view} .forma-data-view-region");
            await page.Locator($"#before-{view}").FocusAsync();

            // Act — in past whatever a view draws before its items, then out
            Assert.Equal("Row 1", await TabIntoItemsAsync(page));
            await page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(page.Locator($"#after-{view}")).ToBeFocusedAsync();

            // Act — back in, on to the second item, opened from the keyboard, and the third opened by a press
            await page.Keyboard.PressAsync("Shift+Tab");
            await WaitForFocusedItemAsync(page, "Row 1");
            await page.Keyboard.PressAsync(next);
            await WaitForFocusedItemAsync(page, "Row 2");
            await Assertions.Expect(opened).ToHaveTextAsync(string.Empty);
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(opened).ToHaveTextAsync("Row 2");
            await items.Nth(2).GetByText("Row 3", new() { Exact = true }).ClickAsync();
            await Assertions.Expect(opened).ToHaveTextAsync("Row 3");
            await WaitForFocusedItemAsync(page, "Row 3");
            await Assertions.Expect(selected).ToHaveTextAsync("0");

            // Act — the selection mode on, the focus staying on the third item's box and the items leaving the Tab sequence to their boxes,
            // then the fourth's checked from the keyboard and opened
            await page.Locator($"#select-{view}").ClickAsync();
            await Assertions.Expect(items.Nth(2).Locator("input[type=checkbox]")).ToBeFocusedAsync();
            await Assertions.Expect(page.Locator($"#{view} [data-item-root][tabindex]")).ToHaveCountAsync(0);
            await page.Keyboard.PressAsync(next);
            await WaitForFocusedItemAsync(page, "Row 4");
            await page.Keyboard.PressAsync("Space");
            await Assertions.Expect(selected).ToHaveTextAsync("1");
            await Assertions.Expect(items.Nth(3).Locator("input[type=checkbox]")).ToBeCheckedAsync();
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(opened).ToHaveTextAsync("Row 4");

            // Act — the selection mode off and on again, the focus staying on the fourth item, then the fifth checked by a press
            await page.Locator($"#select-{view}").ClickAsync();
            await Assertions.Expect(items.Nth(3)).ToBeFocusedAsync();
            await page.Locator($"#select-{view}").ClickAsync();
            await Assertions.Expect(items.Nth(3).Locator("input[type=checkbox]")).ToBeFocusedAsync();
            await items.Nth(4).GetByText("Row 5", new() { Exact = true }).ClickAsync();

            // Assert — the press checked the fifth, its box drawn checked, and opened nothing
            await Assertions.Expect(selected).ToHaveTextAsync("2");
            await Assertions.Expect(items.Nth(4).Locator("input[type=checkbox]")).ToBeCheckedAsync();
            await Assertions.Expect(opened).ToHaveTextAsync("Row 4");
        }

        // Presses Tab until the focus stands on an item, a view drawing a few controls of its own before them.
        private static async Task<string?> TabIntoItemsAsync(IPage page)
        {
            for (var press = 0; press < 4; press++)
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

    public sealed class DataListAppend(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_append_the_next_page_from_its_action_until_none_is_left(string runtime)
        {
            // Arrange — twelve items, five at a time
            var (context, page) = await OpenAsync(host, runtime, "data-list-append");
            await using var _ = context;
            var manual = page.Locator("#manual");
            var names = manual.Locator(".forma-data-list-item-name");
            await Assertions.Expect(names).ToHaveCountAsync(5);

            // Act
            await manual.Locator(".forma-data-view-more button").ClickAsync();
            await Assertions.Expect(names).ToHaveCountAsync(10);
            await manual.Locator(".forma-data-view-more button").ClickAsync();

            // Assert
            await Assertions.Expect(names).ToHaveCountAsync(12);
            await Assertions.Expect(manual.Locator(".forma-data-view-more")).ToHaveCountAsync(0);
            await Assertions.Expect(manual.Locator(".forma-data-view-status")).ToHaveTextAsync("Showing 12 of 12");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_append_the_next_page_as_its_box_scrolls_to_the_end_of_its_items(string runtime)
        {
            // Arrange — sixty rows, ten at a time, in a box showing a few, its end watched and every page asked for answered
            var (context, page) = await OpenAsync(host, runtime, "data-list-append");
            await using var _ = context;
            var names = page.Locator("#automatic tbody tr.forma-data-grid-row");
            var more = page.Locator("#automatic .forma-data-view-more");
            await WaitForObservationAsync(page, "#automatic .forma-data-view-more", "intersection");
            await Assertions.Expect(more.Locator("button")).Not.ToHaveAttributeAsync("aria-busy", "true");
            var shown = await names.CountAsync();

            // Act — the end of the items scrolled into view, through whichever box holds it
            await more.ScrollIntoViewIfNeededAsync();

            // Assert — a page at least, and more where the end of the items stays in view after it
            await Assertions.Expect(names.Nth(shown + 9)).ToBeAttachedAsync();
        }
    }

    public sealed class DataSortMenu(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_sort_the_list_ascending_by_a_choice_that_opens_ascending(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "data-sort-menu");
            await using var _ = context;
            var names = page.Locator("#check .forma-data-list-item-name");
            await Assertions.Expect(names).ToHaveTextAsync(["Hammer", "Atlas", "Wrench"]);

            // Act
            await ChooseAsync(page, "name");

            // Assert
            await Assertions.Expect(names).ToHaveTextAsync(["Atlas", "Hammer", "Wrench"]);
            await Assertions.Expect(page.Locator("#check .forma-data-sort-menu-text")).ToHaveTextAsync("Name");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_sort_the_list_descending_by_a_choice_that_opens_descending(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "data-sort-menu");
            await using var _ = context;
            var names = page.Locator("#check .forma-data-list-item-name");
            await Assertions.Expect(names).ToHaveTextAsync(["Hammer", "Atlas", "Wrench"]);

            // Act
            await ChooseAsync(page, "price");

            // Assert
            await Assertions.Expect(names).ToHaveTextAsync(["Wrench", "Hammer", "Atlas"]);
        }

        // Opens the menu and chooses the sort it names, once the menu stands in the top layer.
        private static async Task ChooseAsync(IPage page, string sort)
        {
            var trigger = page.Locator("#check .forma-data-sort-menu");
            await trigger.ClickAsync();
            await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
            var choice = page.Locator($"[data-sort={sort}]");
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = choice }));
            await choice.ClickAsync();
        }
    }
}
