namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The feedback components' main interactions, in every runtime.</summary>
public sealed class FeedbackTests
{
    public sealed class CapacityIndicator(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_name_its_state_as_the_count_nears_and_reaches_the_limit(string runtime)
        {
            // Arrange — a first seat added brings the count near the limit
            var (context, page) = await OpenAsync(host, runtime, "capacity-indicator");
            await using var _ = context;
            var indicator = page.Locator("#capacity");
            var add = page.Locator("#add");
            await add.ClickAsync();
            await Assertions.Expect(indicator).ToHaveAccessibleNameAsync("Seats");
            await Assertions.Expect(indicator).ToHaveAttributeAsync("aria-valuenow", "8");
            await Assertions.Expect(indicator).ToHaveAttributeAsync("aria-valuetext", "8 of 10, nearly full");
            await Assertions.Expect(indicator).ToContainTextAsync("8 / 10");
            await Assertions.Expect(indicator.GetByText("Nearly Full", new() { Exact = true })).ToBeVisibleAsync();

            // Act
            await add.ClickAsync();
            await add.ClickAsync();

            // Assert
            await Assertions.Expect(indicator).ToHaveAttributeAsync("aria-valuenow", "10");
            await Assertions.Expect(indicator).ToHaveAttributeAsync("aria-valuetext", "10 of 10, full");
            await Assertions.Expect(indicator.GetByText("Full", new() { Exact = true })).ToBeVisibleAsync();
        }
    }

    public sealed class ConfirmDialog(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_answer_the_confirm_service_with_the_button_chosen(string runtime)
        {
            // Arrange — a first confirmation answered by its confirm button
            var (context, page) = await OpenAsync(host, runtime, "confirm-dialog");
            await using var _ = context;
            var dialog = page.GetByRole(AriaRole.Dialog);
            var result = page.Locator("#result");
            await page.Locator("#ask").ClickAsync();
            await Assertions.Expect(dialog).ToContainTextAsync("Delete the draft?");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
            await Assertions.Expect(result).ToHaveTextAsync("Confirmed");
            await Assertions.Expect(dialog).ToBeHiddenAsync();

            // Act
            await page.Locator("#ask").ClickAsync();
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Keep", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("Canceled");
            await Assertions.Expect(dialog).ToBeHiddenAsync();
        }
    }

    public sealed class Dialog(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_and_close_on_escape_and_by_its_close_button(string runtime)
        {
            // Arrange — the dialog opened and closed once on Escape
            var (context, page) = await OpenAsync(host, runtime, "dialog");
            await using var _ = context;
            var dialog = page.Locator("#dialog");
            var result = page.Locator("#result");
            await page.Locator("#open").ClickAsync();
            await Assertions.Expect(dialog).ToBeVisibleAsync();
            await Assertions.Expect(dialog).ToContainTextAsync("Shipped on Monday.");
            await WaitForLayerAsync(dialog);
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(dialog).ToBeHiddenAsync();
            await Assertions.Expect(result).ToHaveTextAsync("1");

            // Act
            await page.Locator("#open").ClickAsync();
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(dialog).ToBeHiddenAsync();
            await Assertions.Expect(result).ToHaveTextAsync("2");
        }
    }

    public sealed class Popover(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_from_its_trigger_and_close_on_escape_and_on_a_press_away(string runtime)
        {
            // Arrange — the details popover opened and dismissed on Escape, leaving the press away to the filters popover so each dismissal comes on a first opening
            var (context, page) = await OpenAsync(host, runtime, "popover");
            await using var _ = context;
            var details = page.Locator("#details");
            var filters = page.Locator("#filters");
            var result = page.Locator("#result");
            await page.Locator("#details-trigger").ClickAsync();
            await Assertions.Expect(details).ToBeVisibleAsync();
            await Assertions.Expect(details).ToHaveTextAsync("Shipped on Monday.");
            await Assertions.Expect(result).ToHaveTextAsync("Details opened");
            await WaitForLayerAsync(details);
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(details).ToBeHiddenAsync();
            await Assertions.Expect(result).ToHaveTextAsync("Details closed");

            // Act
            await page.Locator("#filters-trigger").ClickAsync();
            await WaitForLayerAsync(filters);
            await page.Locator("#outside").ClickAsync();

            // Assert
            await Assertions.Expect(filters).ToBeHiddenAsync();
            await Assertions.Expect(result).ToHaveTextAsync("Filters closed");
        }
    }

    public sealed class Spinner(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_while_loading_and_hide_once_done(string runtime)
        {
            // Arrange — loading started, with the spinner announcing it
            var (context, page) = await OpenAsync(host, runtime, "spinner");
            await using var _ = context;
            var spinner = page.Locator("#spinner");
            await page.Locator("#load").ClickAsync();
            await Assertions.Expect(spinner).ToBeVisibleAsync();
            await Assertions.Expect(spinner).ToHaveAttributeAsync("role", "status");
            await Assertions.Expect(spinner).ToHaveTextAsync("Loading Orders");

            // Act
            await page.Locator("#load").ClickAsync();

            // Assert
            await Assertions.Expect(spinner).ToBeHiddenAsync();
        }
    }

    public sealed class StatusBadge(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_its_new_status_in_its_text_marker_and_color(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "status-badge");
            await using var _ = context;
            var badge = page.Locator("#badge");

            // Act
            await page.Locator("#publish").ClickAsync();

            // Assert
            await Assertions.Expect(badge).ToHaveTextAsync("Published");
            await Assertions.Expect(badge.Locator(".forma-status-badge-marker")).ToHaveCountAsync(1);
            await Assertions.Expect(badge).ToHaveAttributeAsync("style", "--forma-status-badge-fg-color: #198754;");
        }
    }

    public sealed class ToastContainer(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_a_raised_toast_until_it_is_dismissed(string runtime)
        {
            // Arrange — a toast raised through the toast service
            var (context, page) = await OpenAsync(host, runtime, "toast-container");
            await using var _ = context;
            var toasts = page.Locator("#toasts");
            var toast = toasts.GetByText("Draft saved.");
            await page.Locator("#save").ClickAsync();
            await Assertions.Expect(toast).ToBeVisibleAsync();

            // Act
            await toasts.GetByRole(AriaRole.Button, new() { Name = "Dismiss" }).ClickAsync();

            // Assert
            await Assertions.Expect(toast).ToBeHiddenAsync();
        }
    }
}
