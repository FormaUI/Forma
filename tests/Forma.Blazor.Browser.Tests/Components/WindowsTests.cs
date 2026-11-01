namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The windows' interactions, in every runtime.</summary>
public sealed class WindowsTests
{
    public sealed class Window(BrowserHost host)
    {
        // Whether the window named by its id is the topmost element at a point where Alpha and Beta overlap.
        private const string IsInFrontAtOverlap = "id => document.elementFromPoint(320, 280)?.closest('[role=dialog]')?.id === id";

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bring_the_window_pressed_to_the_front_and_fall_back_to_the_other_once_it_closes(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "window");
            await using var _ = context;
            var alpha = page.Locator("#alpha");
            var beta = page.Locator("#beta");
            var result = page.Locator("#result");
            await page.Locator("#open-alpha").ClickAsync();
            await Assertions.Expect(alpha).ToBeVisibleAsync();
            await Assertions.Expect(result).ToHaveTextAsync("Alpha");
            await page.Locator("#open-beta").ClickAsync();
            await Assertions.Expect(beta).ToBeVisibleAsync();
            await Assertions.Expect(result).ToHaveTextAsync("Beta");
            await page.WaitForFunctionAsync(IsInFrontAtOverlap, "beta");

            // Act
            await alpha.ClickAsync(new() { Position = new() { X = 60, Y = 180 } });
            await Assertions.Expect(result).ToHaveTextAsync("Alpha");
            await page.WaitForFunctionAsync(IsInFrontAtOverlap, "alpha");
            await alpha.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(alpha).ToHaveCountAsync(0);
            await Assertions.Expect(result).ToHaveTextAsync("Beta");
            await Assertions.Expect(beta).ToContainClassAsync("is-active");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_take_the_focus_as_it_opens_and_move_by_a_drag_of_its_title(string runtime)
        {
            // Arrange — Alpha opened, its script handing it the focus
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "window");
            await using var _ = context;
            var alpha = page.Locator("#alpha");
            await page.Locator("#open-alpha").ClickAsync();
            await page.WaitForFunctionAsync("() => document.getElementById('alpha')?.contains(document.activeElement) === true");

            // The frame takes hold of a drag as its own script attaches, apart from the window's, which can hand the focus first.
            await ComponentChecks.WaitForInputScopeAsync(page, "#alpha .forma-frame");
            var title = (await alpha.Locator(".forma-frame-drag-region").BoundingBoxAsync())!;
            var x = title.X + (title.Width / 2);
            var y = title.Y + (title.Height / 2);

            // Act
            await page.Mouse.MoveAsync(x, y);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(x + 60, y + 40, new() { Steps = 4 });
            await page.Mouse.UpAsync();

            // Assert
            await Assertions.Expect(page.Locator("#placement")).ToHaveTextAsync("100,160,360,240");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_stand_over_a_scrim_and_hold_the_focus_within_itself_while_modal(string runtime)
        {
            // Arrange — the modal window opened on its scrim, the focus on its last control
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "window");
            await using var _ = context;
            var modal = page.Locator("#modal");
            await page.Locator("#open-modal").ClickAsync();
            await Assertions.Expect(modal).ToHaveAttributeAsync("aria-modal", "true");
            await page.WaitForFunctionAsync("() => document.querySelector('.forma-window-scrim')?.matches(':popover-open') === true");
            await page.WaitForFunctionAsync("() => document.getElementById('modal')?.contains(document.activeElement) === true");
            await page.Locator("#modal-last").FocusAsync();

            // Act
            await page.Keyboard.PressAsync("Tab");

            // Assert
            await page.WaitForFunctionAsync("() => document.getElementById('modal')?.contains(document.activeElement) === true");
        }
    }

    public sealed class RestArea(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_restore_a_minimized_window_from_its_handle(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "rest-area");
            await using var _ = context;
            var notes = page.Locator("#notes");
            var handle = page.Locator("#rest-area").GetByRole(AriaRole.Button, new() { Name = "Notes", Exact = true });
            var result = page.Locator("#result");
            await page.Locator("#open").ClickAsync();
            await Assertions.Expect(notes).ToBeVisibleAsync();
            await notes.GetByRole(AriaRole.Button, new() { Name = "Minimize", Exact = true }).ClickAsync();
            await Assertions.Expect(notes).ToBeHiddenAsync();
            await Assertions.Expect(result).ToHaveTextAsync("minimized");
            await Assertions.Expect(handle).ToContainClassAsync("is-minimized");

            // Act
            await handle.ClickAsync();

            // Assert
            await Assertions.Expect(notes).ToBeVisibleAsync();
            await Assertions.Expect(result).ToHaveTextAsync("restored");
            await Assertions.Expect(handle).Not.ToContainClassAsync("is-minimized");
            await Assertions.Expect(handle).ToHaveAttributeAsync("aria-pressed", "true");
        }
    }
}
