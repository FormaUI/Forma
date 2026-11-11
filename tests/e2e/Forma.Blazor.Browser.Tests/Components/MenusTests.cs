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
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = menu }));
            await Assertions.Expect(menu).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#rename")).ToBeFocusedAsync();
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hold_tab_within_itself_and_wrap_at_either_end(string runtime)
        {
            // Arrange — the menu opened and moved to its last item
            var (context, page) = await OpenAsync(host, runtime, "drop-down-menu");
            await using var _ = context;
            await page.Locator("#menu").ClickAsync();
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = page.GetByRole(AriaRole.Menu) }));
            await Assertions.Expect(page.Locator("#rename")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("End");
            await Assertions.Expect(page.Locator("#pin")).ToBeFocusedAsync();

            // Act
            await page.Keyboard.PressAsync("Tab");

            // Assert
            await Assertions.Expect(page.Locator("#rename")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await Assertions.Expect(page.Locator("#pin")).ToBeFocusedAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Menu)).ToBeVisibleAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_move_to_an_item_drawn_ahead_of_the_rest_while_open_by_its_place_on_the_page(string runtime)
        {
            // Arrange — Pin, the last item, run by keyboard, which keeps the menu open and draws Unpin first
            var (context, page) = await OpenAsync(host, runtime, "drop-down-menu");
            await using var _ = context;
            await page.Locator("#menu").ClickAsync();
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = page.GetByRole(AriaRole.Menu) }));
            await Assertions.Expect(page.Locator("#rename")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("End");
            await Assertions.Expect(page.Locator("#pin")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(page.Locator("#unpin")).ToBeVisibleAsync();

            // Act
            await page.Keyboard.PressAsync("Home");

            // Assert
            await Assertions.Expect(page.Locator("#unpin")).ToBeFocusedAsync();
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
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = menu }));
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

        // A share surface standing in for the platform's, recording what it is handed.
        private const string RecordingShareSurface = "Object.defineProperty(Navigator.prototype, 'share', { configurable: true, value(data) { (window.formaShared ??= []).push(data); return Promise.resolve(); } });";

        // A print standing in for the browser's dialog, counting its calls.
        private const string CountingPrint = "window.print = () => { window.formaPrints = (window.formaPrints ?? 0) + 1; };";

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
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = menu }));
            await Assertions.Expect(menu.Locator(".forma-share-menu-primary [role=menuitem][href]")).ToHaveTextAsync(Networks);

            var network = menu.GetByRole(AriaRole.Menuitem, new() { Name = "X", Exact = true });
            const string Composer = "https://x.com/intent/post?url=https%3A%2F%2Fexample.com%2Freports%2Fquarterly&text=Quarterly%20Report";

            // Act and Assert — a web view hands another site's address to the system's browser, which the check host records in its place
            if (runtime is "webview" or "android")
            {
                await network.ClickAsync();
                await page.WaitForFunctionAsync("composer => window.formaOpenedExternally.includes(composer)", Composer);
            }
            else
            {
                var composer = await page.RunAndWaitForPopupAsync(() => network.ClickAsync());
                await composer.WaitForLoadStateAsync();
                Assert.Equal(Composer, composer.Url);
            }

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
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = menu }));
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_offer_the_platform_s_share_surface_where_the_browser_has_one_and_hand_it_the_item(string runtime)
        {
            // Arrange — a share surface standing in for the platform's, which records what it was handed
            var (context, page) = await OpenAsync(host, runtime, "share-menu", page => page.AddInitScriptAsync(RecordingShareSurface));
            await using var _ = context;
            var menu = page.GetByRole(AriaRole.Menu);
            await page.Locator("#share").ClickAsync();
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = menu }));
            var share = menu.Locator(".forma-share-menu-primary [role=menuitem]").First;
            await Assertions.Expect(share).ToHaveTextAsync("Share");

            // Act
            await share.ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("() => window.formaShared?.length === 1");
            Assert.Equal("Quarterly Report|https://example.com/reports/quarterly", await page.EvaluateAsync<string>("() => `${window.formaShared[0].title}|${window.formaShared[0].url}`"));
            await Assertions.Expect(menu).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_copy_the_item_s_link_to_the_clipboard(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "share-menu", page => page.AddInitScriptAsync(RecordingClipboard));
            await using var _ = context;
            await AllowClipboardWriteAsync(context, runtime);
            var menu = page.GetByRole(AriaRole.Menu);
            await page.Locator("#share").ClickAsync();

            // Act
            await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Copy Link", Exact = true }).ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("() => (window.formaCopied ?? []).includes('https://example.com/reports/quarterly')");
            await Assertions.Expect(menu).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_print_the_page(string runtime)
        {
            // Arrange — the page's print standing in for the browser's dialog, which counts its calls
            var (context, page) = await OpenAsync(host, runtime, "share-menu", page => page.AddInitScriptAsync(CountingPrint));
            await using var _ = context;
            var menu = page.GetByRole(AriaRole.Menu);
            await page.Locator("#share").ClickAsync();

            // Act
            await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Print", Exact = true }).ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("() => window.formaPrints === 1");
            await Assertions.Expect(menu).ToBeHiddenAsync();
        }
    }
}
