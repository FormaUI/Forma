namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The feedback components' interactions, in every runtime.</summary>
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
            await WaitForLayerAsync(dialog);
            await Assertions.Expect(dialog).ToBeVisibleAsync();
            await Assertions.Expect(dialog).ToContainTextAsync("Shipped on Monday.");
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_focus_its_body_as_it_opens_and_close_on_a_close_request_and_on_a_press_on_its_scrim(string runtime)
        {
            // Arrange — the dialog opened on the field in its body and closed once by the platform's own request, as a back gesture makes it
            var (context, page) = await OpenAsync(host, runtime, "dialog");
            await using var _ = context;
            var dialog = page.Locator("#dialog");
            var result = page.Locator("#result");
            await page.Locator("#open").ClickAsync();
            await WaitForLayerAsync(dialog);
            await Assertions.Expect(page.Locator("#note")).ToBeFocusedAsync();
            await dialog.EvaluateAsync("dialog => dialog.requestClose()");
            await Assertions.Expect(dialog).ToBeHiddenAsync();
            await Assertions.Expect(result).ToHaveTextAsync("1");
            await page.Locator("#open").ClickAsync();
            await Assertions.Expect(page.Locator("#note")).ToBeFocusedAsync();

            // Act
            await page.Mouse.ClickAsync(4, 4);

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
            await WaitForLayerAsync(details);
            await Assertions.Expect(details).ToBeVisibleAsync();
            await Assertions.Expect(details).ToHaveTextAsync("Shipped on Monday.");
            await Assertions.Expect(result).ToHaveTextAsync("Details opened");
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_below_its_anchor_at_the_anchor_s_start(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "popover");
            await using var _ = context;
            var details = page.Locator("#details");
            var trigger = page.Locator("#details-trigger");

            // Act — the layer is stamped as the popover registers, ahead of the call that shows and places it
            await trigger.ClickAsync();
            await WaitForLayerAsync(details);
            await Assertions.Expect(details).ToBeVisibleAsync();

            // Assert — four pixels below the anchor, its start edge held four pixels within the viewport
            var anchor = (await page.Locator("span", new() { Has = trigger }).BoundingBoxAsync())!;
            var placed = (await details.BoundingBoxAsync())!;
            Assert.Equal(anchor.Y + anchor.Height + 4, placed.Y, 1.0);
            Assert.Equal(Math.Max(4, anchor.X), placed.X, 1.0);
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
        // Records in window.formaToastStates each change of whether the toast with the message given shows in the container, 'shown' as
        // it does and 'gone' as it no longer does, as the page changes it.
        private const string RecordingToastStates = """
            (toasts, message) => {
                window.formaToastStates = [];
                const isShown = () => [...toasts.querySelectorAll('.forma-toast')].some(toast => toast.textContent.includes(message) && toast.checkVisibility());
                new MutationObserver(() => {
                    const state = isShown() ? 'shown' : 'gone';

                    if (state !== (window.formaToastStates.at(-1) ?? 'gone')) {
                        window.formaToastStates.push(state);
                    }
                }).observe(toasts, { childList: true, subtree: true, characterData: true });
            }
            """;

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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_dismiss_a_toast_once_its_duration_passes(string runtime)
        {
            // Arrange — the toast's showing and leaving recorded as the page makes them, so its two seconds on screen need no catching
            var (context, page) = await OpenAsync(host, runtime, "toast-container");
            await using var _ = context;
            await page.Locator("#toasts").EvaluateAsync(RecordingToastStates, "Published.");

            // Act
            await page.Locator("#publish").ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("() => window.formaToastStates.join(' ') === 'shown gone'", null, new() { Timeout = 10000 });
        }
    }

    public sealed class Tooltip(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_a_control_s_tooltip_as_the_keyboard_reaches_it_and_hide_it_on_escape_leaving_the_focus(string runtime)
        {
            // Arrange — the keyboard on the button before
            var (context, page) = await OpenAsync(host, runtime, "tooltip");
            await using var _ = context;
            var save = page.Locator("#save");
            var tooltip = page.Locator("#save-tip");
            await page.Locator("#before").FocusAsync();

            // Act — Tab to the button its tooltip describes, then Escape
            await page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(save).ToBeFocusedAsync();
            await Assertions.Expect(tooltip).ToBeVisibleAsync();
            await page.Keyboard.PressAsync("Escape");

            // Assert
            await Assertions.Expect(tooltip).ToBeHiddenAsync();
            await Assertions.Expect(save).ToBeFocusedAsync();
            await Assertions.Expect(save).ToHaveAccessibleDescriptionAsync("Saved drafts are kept for thirty days.");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_a_control_s_tooltip_once_the_pointer_rests_on_it_and_keep_it_while_the_pointer_moves_onto_it(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "tooltip");
            await using var _ = context;
            var tooltip = page.Locator("#save-tip");

            // Act — the pointer resting on the button, then moving onto the tooltip
            await page.Locator("#save").HoverAsync();
            await Assertions.Expect(tooltip).ToBeVisibleAsync();
            await tooltip.HoverAsync();
            await Assertions.Expect(tooltip).ToBeVisibleAsync();
            await page.Locator("#after").HoverAsync();

            // Assert — gone once the pointer has left both
            await Assertions.Expect(tooltip).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_a_control_s_tooltip_on_a_tap_and_hide_it_on_a_tap_elsewhere(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "tooltip");
            await using var _ = context;
            var tooltip = page.Locator("#save-tip");
            var tap = new { pointerType = "touch", isPrimary = true, bubbles = true, composed = true };

            // Act — a finger's press on the button, then on the button after
            await page.Locator("#save").DispatchEventAsync("pointerdown", tap);
            await Assertions.Expect(tooltip).ToBeVisibleAsync();
            await page.Locator("#after").DispatchEventAsync("pointerdown", tap);

            // Assert
            await Assertions.Expect(tooltip).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(RoutedRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_start_a_page_whose_host_refuses_the_tooltip_s_script_its_controls_still_described(string runtime)
        {
            // Arrange — a file server refusing the tooltip's script, which Blazor's start waits for
            var isRefused = false;

            // Act — the page opens once it is interactive
            var (context, page) = await OpenAsync(host, runtime, "tooltip", page => page.RouteAsync("**/scripts/components/Tooltip*.js", route =>
            {
                isRefused = true;

                return route.AbortAsync();
            }));
            await using var _ = context;

            // Assert
            Assert.True(isRefused);
            await Assertions.Expect(page.Locator("#save")).ToHaveAccessibleDescriptionAsync("Saved drafts are kept for thirty days.");
            await Assertions.Expect(page.Locator("#save-tip")).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hang_a_tooltip_centered_on_its_control_reading_right_to_left_in_a_right_to_left_region(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "tooltip");
            await using var _ = context;
            var control = page.Locator("#rtl-save");
            var tooltip = page.Locator("#rtl-save-tip");

            // Act
            await control.HoverAsync();
            await Assertions.Expect(tooltip).ToBeVisibleAsync();

            // Assert — its middle over the control's, its text right to left
            var controlBox = (await control.BoundingBoxAsync())!;
            var tooltipBox = (await tooltip.BoundingBoxAsync())!;
            Assert.Equal(controlBox.X + (controlBox.Width / 2), tooltipBox.X + (tooltipBox.Width / 2), 1.0);
            Assert.Equal("rtl", await tooltip.EvaluateAsync<string>("tooltip => getComputedStyle(tooltip).direction"));
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_keep_a_button_disabled_with_a_reason_in_reach_and_inert_showing_its_reason(string runtime)
        {
            // Arrange — the keyboard on the button before
            var (context, page) = await OpenAsync(host, runtime, "tooltip");
            await using var _ = context;
            var publish = page.Locator("#publish");
            await page.Locator("#save").FocusAsync();

            // Act — Tab to Publish, press it from the keyboard and with the pointer
            await page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(publish).ToBeFocusedAsync();
            await Assertions.Expect(page.Locator("#publish-disabled-reason")).ToBeVisibleAsync();
            await page.Keyboard.PressAsync("Enter");
            await publish.ClickAsync(new() { Force = true });

            // Assert — nothing ran, and its reason describes it
            await Assertions.Expect(publish).ToHaveAttributeAsync("aria-disabled", "true");
            await Assertions.Expect(publish).ToHaveAccessibleDescriptionAsync("Give the draft a title before you publish it.");
            await Assertions.Expect(page.Locator("#result")).ToBeEmptyAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_each_input_s_reason_as_the_keyboard_reaches_it_and_keep_the_input_from_changing(string runtime)
        {
            // Arrange — the keyboard on the button disabled with a reason, before the inputs
            var (context, page) = await OpenAsync(host, runtime, "tooltip");
            await using var _ = context;
            await page.Locator("#publish").FocusAsync();

            // Act — Tab to each input, its reason shown, then the keys that would change it pressed
            foreach (var id in (string[])["club-name", "plan", "newsletter", "seats"])
            {
                await page.Keyboard.PressAsync("Tab");
                await Assertions.Expect(page.Locator($"#{id}")).ToBeFocusedAsync();
                await Assertions.Expect(page.Locator($"#{id}-disabled-reason")).ToBeVisibleAsync();
            }

            await page.Locator("#club-name").FocusAsync();
            await page.Keyboard.TypeAsync("x");
            await page.Locator("#plan").PressAsync("Enter");
            await page.Locator("#newsletter").PressAsync("Space");
            await page.Locator("#seats").PressAsync("ArrowRight");

            // Assert — each as it was, and the select's list never opened
            await Assertions.Expect(page.Locator("#club-name")).ToHaveValueAsync("Harbor FC");
            await Assertions.Expect(page.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#newsletter")).Not.ToBeCheckedAsync();
            await Assertions.Expect(page.Locator("#seats")).ToHaveValueAsync("4");
            await Assertions.Expect(page.Locator("#club-name")).ToHaveAccessibleDescriptionAsync("Club names are set by the federation.");
        }

        [Fact]
        public async Task Should_show_a_button_s_reason_for_being_disabled_as_the_keyboard_reaches_it_on_a_static_page()
        {
            // Arrange — the page rendered statically, the keyboard on the button before
            var (context, page) = await host.OpenPageAsync(await host.GetComponentsAddressAsync());
            await using var _ = context;
            await page.GotoAsync("static/tooltip");
            await Assertions.Expect(page.Locator("#runtime")).ToHaveTextAsync("Static False");
            var publish = page.Locator("#publish");
            await page.Locator("#save").FocusAsync();

            // Act — the page's script shows the reason no renderer draws
            await page.Keyboard.PressAsync("Tab");

            // Assert
            await Assertions.Expect(publish).ToBeFocusedAsync();
            await Assertions.Expect(page.Locator("#publish-disabled-reason")).ToBeVisibleAsync();
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(page.Locator("#publish-disabled-reason")).ToBeHiddenAsync();
        }
    }
}
