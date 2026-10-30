namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The menus' main interactions, in every runtime.</summary>
public sealed class MenusTests
{
    public sealed class DropDownMenu(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_move_through_its_items_by_arrow_keys_and_run_the_focused_one(string runtime)
        {
            // Arrange — the menu opened on its first item, then moved down twice, out of the group and past the divider, and up once
            var (context, page) = await OpenAsync(host, runtime, "drop-down-menu");
            await using var _ = context;
            var trigger = page.Locator("#menu");
            var menu = page.GetByRole(AriaRole.Menu);
            await trigger.ClickAsync();
            await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
            await Assertions.Expect(menu).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#rename")).ToBeFocusedAsync();
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = menu }));
            await page.Keyboard.PressAsync("ArrowDown");
            await page.Keyboard.PressAsync("ArrowDown");
            await Assertions.Expect(page.Locator("#delete")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("ArrowUp");
            await Assertions.Expect(page.Locator("#duplicate")).ToBeFocusedAsync();

            // Act
            await page.Keyboard.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Duplicate");
            await Assertions.Expect(menu).ToBeHiddenAsync();
            await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
            await Assertions.Expect(trigger).ToBeFocusedAsync();
        }
    }

    public sealed class OverflowMenu(BrowserHost host)
    {
        private static readonly string[] OverflowCommands = ["Pin", "Archive", "Delete"];

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_its_items_and_run_the_one_chosen(string runtime)
        {
            // Arrange — the menu opened from its ellipsis, listing its items
            var (context, page) = await OpenAsync(host, runtime, "overflow-menu");
            await using var _ = context;
            var trigger = page.Locator("#more");
            var menu = page.GetByRole(AriaRole.Menu);
            await trigger.ClickAsync();
            await Assertions.Expect(trigger).ToHaveAccessibleNameAsync("More Actions");
            await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
            await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem)).ToHaveTextAsync(OverflowCommands);

            // Act
            await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Archive", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Archive");
            await Assertions.Expect(menu).ToBeHiddenAsync();
            await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        }
    }

    public sealed class ShareMenu(BrowserHost host)
    {
        private static readonly string[] Networks = ["Facebook", "X", "WhatsApp", "Telegram", "Bluesky", "Reddit", "Snapchat", "LinkedIn"];

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_list_the_networks_that_take_a_link_and_open_the_one_chosen_on_the_item(string runtime)
        {
            // Arrange — the menu opened on the link it shares, X's composer answered on this machine
            var (context, page) = await OpenAsync(host, runtime, "share-menu");
            await using var _ = context;
            await context.RouteAsync("https://x.com/**", route => route.FulfillAsync(new() { ContentType = "text/html", Body = "<title>X</title>" }));
            var menu = page.GetByRole(AriaRole.Menu);
            await page.Locator("#share").ClickAsync();
            await Assertions.Expect(menu.Locator(".forma-share-menu-primary [role=menuitem][href]")).ToHaveTextAsync(Networks);

            // Act
            var composer = await page.RunAndWaitForPopupAsync(() => menu.GetByRole(AriaRole.Menuitem, new() { Name = "X", Exact = true }).ClickAsync());

            // Assert
            await composer.WaitForLoadStateAsync();
            Assert.Equal("https://x.com/intent/post?url=https%3A%2F%2Fexample.com%2Freports%2Fquarterly&text=Quarterly%20Report", composer.Url);
            await Assertions.Expect(menu).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_list_its_commands_and_raise_the_action_behind_more(string runtime)
        {
            // Arrange — the menu opened on the item it shares, listing its own commands, with More expanded
            var (context, page) = await OpenAsync(host, runtime, "share-menu");
            await using var _ = context;
            var menu = page.GetByRole(AriaRole.Menu);
            await page.Locator("#share").ClickAsync();
            await Assertions.Expect(menu).ToContainTextAsync("Quarterly Report");
            await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Email", Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Copy Link", Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Print", Exact = true })).ToBeVisibleAsync();
            await menu.GetByRole(AriaRole.Menuitem, new() { Name = "More", Exact = true }).ClickAsync();
            await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Less", Exact = true })).ToHaveAttributeAsync("aria-expanded", "true");

            // Act
            await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Archive", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("archive");
            await Assertions.Expect(menu).ToBeHiddenAsync();
        }
    }
}
