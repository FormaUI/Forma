namespace Forma.Blazor.Browser.Tests.Components;

using System.Globalization;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The layout components' main interactions, in every runtime.</summary>
public sealed class LayoutTests
{
    public sealed class Backdrop(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_render_its_content_in_the_variant_it_is_given(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "backdrop");
            await using var _ = context;
            var backdrop = page.Locator("#backdrop");
            await Assertions.Expect(backdrop).ToHaveTextAsync("Quarterly figures");
            await Assertions.Expect(backdrop).Not.ToContainClassAsync("forma-backdrop-raised");

            // Act
            await page.Locator("#raise").ClickAsync();

            // Assert
            await Assertions.Expect(backdrop).ToContainClassAsync("forma-backdrop-raised");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Raised");
        }
    }

    public sealed class Chrome(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_be_named_by_its_title_and_run_its_title_bar_command(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "chrome");
            await using var _ = context;
            var chrome = page.Locator("#chrome");
            await Assertions.Expect(chrome).ToHaveRoleAsync(AriaRole.Region);
            await Assertions.Expect(chrome).ToHaveAccessibleNameAsync("Report");
            await Assertions.Expect(chrome).ToContainTextAsync("Quarterly figures");

            // Act
            await chrome.GetByRole(AriaRole.Button, new() { Name = "Refresh", Exact = true }).ClickAsync();
            await page.Locator("#rename").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("refresh");
            await Assertions.Expect(chrome).ToHaveAccessibleNameAsync("Summary");
        }
    }

    public sealed class CommandArea(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_stack_the_command_bar_added_to_it(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "command-area");
            await using var _ = context;
            var area = page.Locator("#command-area");
            await Assertions.Expect(area.Locator(".forma-command-bar")).ToHaveCountAsync(1);

            // Act
            await page.Locator("#add").ClickAsync();

            // Assert
            await Assertions.Expect(area.Locator(".forma-command-bar")).ToHaveCountAsync(2);
            await Assertions.Expect(area.GetByRole(AriaRole.Toolbar, new() { Name = "Formatting", Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("True");
        }
    }

    public sealed class CommandBar(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_run_the_command_pressed_on_it(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "command-bar");
            await using var _ = context;
            var bar = page.GetByRole(AriaRole.Toolbar, new() { Name = "Clipboard", Exact = true });

            // Act
            await bar.GetByRole(AriaRole.Button, new() { Name = "Copy", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("copy");
        }
    }

    public sealed class Frame(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_stay_within_the_viewport_and_move_and_resize_by_keyboard(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "frame");
            await using var _ = context;
            var viewport = page.ViewportSize;
            Assert.NotNull(viewport);
            var dragRegion = page.Locator("#frame .forma-frame-drag-region");
            var result = page.Locator("#result");

            // The frame's script, once attached, pulls the frame placed past the viewport's end back within it.
            await Assertions.Expect(result).ToHaveTextAsync(FormatPlacement(viewport.Width - 320, 96, 320, 192));

            // Act
            await dragRegion.PressAsync("ArrowLeft");
            await Assertions.Expect(result).ToHaveTextAsync(FormatPlacement(viewport.Width - 336, 96, 320, 192));
            await dragRegion.PressAsync("ArrowDown");
            await Assertions.Expect(result).ToHaveTextAsync(FormatPlacement(viewport.Width - 336, 112, 320, 192));
            await dragRegion.PressAsync("Shift+ArrowDown");

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync(FormatPlacement(viewport.Width - 336, 112, 320, 208));
        }

        private static string FormatPlacement(int left, int top, int width, int height)
            => string.Create(CultureInfo.InvariantCulture, $"{left},{top},{width},{height}");
    }

    public sealed class FreezePane(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_scroll_forward_and_hold_its_frozen_part_at_the_start(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "freeze-pane");
            await using var _ = context;
            var viewport = page.Locator("#freeze-pane .forma-freeze-pane-viewport");
            await WaitForInputScopeAsync(page, "#freeze-pane .forma-freeze-pane-arrow-forward");

            // Act
            await page.Locator("#freeze-pane .forma-freeze-pane-arrow-forward").ClickAsync();

            // Assert
            await Assertions.Expect(viewport).Not.ToHaveJSPropertyAsync("scrollLeft", 0);
            await Assertions.Expect(page.Locator("#freeze-pane .forma-freeze-pane-arrow-back")).ToBeVisibleAsync();
            var viewportBox = await viewport.BoundingBoxAsync();
            var frozenBox = await page.Locator("#frozen").BoundingBoxAsync();
            Assert.NotNull(viewportBox);
            Assert.NotNull(frozenBox);
            Assert.Equal(viewportBox.X, frozenBox.X, 1.0);
        }
    }

    public sealed class Splitter(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_resize_the_pane_by_its_arrow_keys(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "splitter");
            await using var _ = context;
            var splitter = page.Locator("#splitter");
            await WaitForInputScopeAsync(page, "#splitter");

            // Act
            await splitter.PressAsync("ArrowRight");
            await splitter.PressAsync("ArrowRight");
            await splitter.PressAsync("ArrowRight");
            await splitter.PressAsync("ArrowLeft");

            // Assert
            await Assertions.Expect(splitter).ToHaveAttributeAsync("aria-valuenow", "14");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("14");
            await Assertions.Expect(page.Locator("#pane")).ToHaveCSSAsync("width", "224px");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_follow_a_drag_and_end_it_where_the_pointer_is_let_go_off_the_bar(string runtime)
        {
            // Arrange — the drag's script routes the press once it has attached
            var (context, page) = await OpenAsync(host, runtime, "splitter");
            await using var _ = context;
            var splitter = page.Locator("#splitter");
            await WaitForInputRouteAsync(page, "#splitter", "splitter.resize");
            var bar = (await splitter.BoundingBoxAsync())!;
            var x = bar.X + (bar.Width / 2);
            var y = bar.Y + (bar.Height / 2);

            // Act — 64 pixels along the inline axis, four units, then let go well below the bar
            await page.Mouse.MoveAsync(x, y);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(x + 64, y, new() { Steps = 4 });
            await page.Mouse.MoveAsync(x + 64, y + 200);
            await page.Mouse.UpAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("16");
            await Assertions.Expect(splitter).ToHaveAttributeAsync("aria-valuenow", "16");
            await Assertions.Expect(splitter).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-resizing"));
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_return_to_the_size_a_drag_started_from_on_escape(string runtime)
        {
            // Arrange — a drag of four units under way
            var (context, page) = await OpenAsync(host, runtime, "splitter");
            await using var _ = context;
            var splitter = page.Locator("#splitter");
            await WaitForInputRouteAsync(page, "#splitter", "splitter.resize");
            var bar = (await splitter.BoundingBoxAsync())!;
            var x = bar.X + (bar.Width / 2);
            var y = bar.Y + (bar.Height / 2);
            await page.Mouse.MoveAsync(x, y);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(x + 64, y, new() { Steps = 4 });
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("16");

            // Act
            await page.Keyboard.PressAsync("Escape");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("12");
            await page.Mouse.UpAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("12");
        }
    }

    public sealed class StackLayout(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_lay_out_its_content_along_the_axis_it_is_given(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "stack-layout");
            await using var _ = context;
            var stack = page.Locator("#stack");
            await Assertions.Expect(stack).ToContainClassAsync("forma-stack-layout-vertical");
            await Assertions.Expect(stack.Locator(".forma-backdrop")).ToHaveCountAsync(2);

            // Act
            await page.Locator("#turn").ClickAsync();

            // Assert
            await Assertions.Expect(stack).ToContainClassAsync("forma-stack-layout-horizontal");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Horizontal");
            var first = await page.Locator("#first").BoundingBoxAsync();
            var second = await page.Locator("#second").BoundingBoxAsync();
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.Y, second.Y, 1.0);
            Assert.True(second.X >= first.X + first.Width, "The second backdrop stands beside the first.");
        }
    }

    public sealed class Tabs(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_select_the_tab_pressed_and_the_tab_the_arrow_keys_reach(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "tabs");
            await using var _ = context;
            var privacy = page.Locator("#tab-privacy");
            var advanced = page.Locator("#tab-advanced");
            await WaitForInputScopeAsync(page, "#tabs .forma-tab-list");

            // Act
            await privacy.ClickAsync();
            await page.Keyboard.PressAsync("ArrowRight");
            await Assertions.Expect(advanced).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("privacy advanced");
            await Assertions.Expect(advanced).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(privacy).ToHaveAttributeAsync("aria-selected", "false");
            await Assertions.Expect(page.Locator("#panel-advanced")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#panel-privacy")).ToBeHiddenAsync();
        }
    }
}
