namespace Forma.Blazor.Browser.Tests.Components;

using System.Globalization;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The layout components' interactions, in every runtime.</summary>
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_move_by_a_drag_of_its_title_and_resize_by_a_drag_of_its_edge(string runtime)
        {
            // Arrange — the frame pulled back within the viewport by its script
            var (context, page) = await OpenAsync(host, runtime, "frame");
            await using var _ = context;
            var viewport = page.ViewportSize!;
            var result = page.Locator("#result");
            await Assertions.Expect(result).ToHaveTextAsync(FormatPlacement(viewport.Width - 320, 96, 320, 192));

            // Act
            await DragAsync(page, page.Locator("#frame .forma-frame-drag-region"), -100, 40);
            await Assertions.Expect(result).ToHaveTextAsync(FormatPlacement(viewport.Width - 420, 136, 320, 192));
            await DragAsync(page, page.Locator("#frame .forma-frame-handle-s"), 0, 32);

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync(FormatPlacement(viewport.Width - 420, 136, 320, 224));
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_keep_within_a_viewport_that_narrows(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "frame");
            await using var _ = context;
            var viewport = page.ViewportSize!;
            var result = page.Locator("#result");
            await Assertions.Expect(result).ToHaveTextAsync(FormatPlacement(viewport.Width - 320, 96, 320, 192));

            // Act
            await page.SetViewportSizeAsync(viewport.Width - 200, viewport.Height);

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync(FormatPlacement(viewport.Width - 520, 96, 320, 192));
        }

        // A drag from the middle of the element by the distance given.
        private static async Task DragAsync(IPage page, ILocator held, float x, float y)
        {
            var box = (await held.BoundingBoxAsync())!;
            var startX = box.X + (box.Width / 2);
            var startY = box.Y + (box.Height / 2);

            await page.Mouse.MoveAsync(startX, startY);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(startX + x, startY + y, new() { Steps = 4 });
            await page.Mouse.UpAsync();
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

        [Theory]
        [InlineData("server")]
        [InlineData("auto-server")]
        public async Task Should_show_a_scroll_made_while_its_circuit_was_away_once_the_circuit_is_back(string runtime)
        {
            // Arrange — the circuit's connection carried through a route the check can drop
            var connections = new List<(IWebSocketRoute Page, IWebSocketRoute Server)>();
            var (context, page) = await OpenAsync(host, runtime, "freeze-pane", page => page.RouteWebSocketAsync("**/_blazor**", connection => connections.Add((connection, connection.ConnectToServer()))));
            await using var _ = context;
            var viewport = page.Locator("#freeze-pane .forma-freeze-pane-viewport");
            var back = page.Locator("#freeze-pane .forma-freeze-pane-arrow-back");
            await WaitForInputScopeAsync(page, "#freeze-pane .forma-freeze-pane-arrow-forward");
            await Assertions.Expect(back).ToBeHiddenAsync();
            var (pageSide, serverSide) = connections[^1];

            // Act — the connection dropped and the pane scrolled while nothing can tell the circuit: the page's reconnection is held at its
            // negotiation until the pane's script has heard the scroll and had the frame it reports in, then the page reconnects on its own
            var scrolled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await page.RouteAsync("**/_blazor/negotiate**", async route =>
            {
                await scrolled.Task;
                await route.ContinueAsync();
            });
            await serverSide.CloseAsync();
            await pageSide.CloseAsync();

            try
            {
                await viewport.EvaluateAsync("viewport => new Promise(resolve => { viewport.addEventListener('scroll', () => requestAnimationFrame(() => requestAnimationFrame(resolve)), { once: true }); viewport.scrollLeft = viewport.scrollWidth; })");
            }
            finally
            {
                scrolled.SetResult();
            }

            // Assert
            await Assertions.Expect(back).ToBeVisibleAsync(new() { Timeout = 30000 });
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

            // Act — each press answered before the next, so the last step's answer is the only way to the size expected
            await splitter.PressAsync("ArrowRight");
            await Assertions.Expect(splitter).ToHaveAttributeAsync("aria-valuenow", "13");
            await splitter.PressAsync("ArrowRight");
            await Assertions.Expect(splitter).ToHaveAttributeAsync("aria-valuenow", "14");
            await splitter.PressAsync("ArrowRight");
            await Assertions.Expect(splitter).ToHaveAttributeAsync("aria-valuenow", "15");
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
        public async Task Should_follow_a_drag_along_the_block_axis_where_it_lies_between_stacked_panes(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "splitter");
            await using var _ = context;
            var splitter = page.Locator("#rows-splitter");
            await WaitForInputRouteAsync(page, "#rows-splitter", "splitter.resize");
            var bar = (await splitter.BoundingBoxAsync())!;
            var x = bar.X + (bar.Width / 2);
            var y = bar.Y + (bar.Height / 2);

            // Act — 64 pixels down, four units
            await page.Mouse.MoveAsync(x, y);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(x, y + 64, new() { Steps = 4 });
            await page.Mouse.UpAsync();

            // Assert
            await Assertions.Expect(page.Locator("#rows-result")).ToHaveTextAsync("12");
            await Assertions.Expect(splitter).ToHaveAttributeAsync("aria-valuenow", "12");
            await Assertions.Expect(splitter).ToHaveAttributeAsync("aria-orientation", "horizontal");
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
        public async Task Should_select_the_tab_pressed_and_the_tab_the_arrow_keys_reach_without_submitting_its_form(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "tabs");
            await using var _ = context;
            var general = page.Locator("#tab-general");
            var privacy = page.Locator("#tab-privacy");
            var advanced = page.Locator("#tab-advanced");
            await WaitForInputScopeAsync(page, "#tabs .forma-tab-list");

            // Act
            await privacy.ClickAsync();
            await page.Keyboard.PressAsync("ArrowRight");
            await Assertions.Expect(advanced).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("privacy advanced");
            await page.Keyboard.PressAsync("Home");
            await Assertions.Expect(general).ToBeFocusedAsync();
            await page.Keyboard.PressAsync(" ");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("privacy advanced general");
            await Assertions.Expect(general).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(advanced).ToHaveAttributeAsync("aria-selected", "false");
            await Assertions.Expect(page.Locator("#panel-general")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#panel-advanced")).ToBeHiddenAsync();
            await Assertions.Expect(page.Locator("#submits")).ToHaveTextAsync("0");
        }
    }
}
