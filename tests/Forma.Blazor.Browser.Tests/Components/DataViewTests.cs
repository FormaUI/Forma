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

            // Act
            await page.SetViewportSizeAsync(360, viewport.Height);
            await Assertions.Expect(notes).ToHaveCountAsync(0);
            await page.SetViewportSizeAsync(viewport.Width, viewport.Height);

            // Assert
            await Assertions.Expect(notes).ToBeVisibleAsync();
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
