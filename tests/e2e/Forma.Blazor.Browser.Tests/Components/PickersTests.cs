namespace Forma.Blazor.Browser.Tests.Components;

using System.Globalization;

/// <summary>The selects', the calendar's and the date and time pickers' main interactions, in every runtime.</summary>
public sealed class PickersTests
{
    public sealed class Select(BrowserHost host)
    {
        // A page the first key typed into holds busy for a second, twice the type-ahead's pause, so each key after it reaches the page late.
        private const string HoldingFirstKey = "let isHeld = false; addEventListener('keydown', () => { if (!isHeld) { isHeld = true; const end = performance.now() + 1000; while (performance.now() < end) { } } }, { capture: true });";

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_choose_an_option_from_the_list_it_opens(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "select");
            await using var _ = context;

            // Act
            await page.Locator("#check").ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = "Closed", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2");
            await Assertions.Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_on_the_option_a_typed_label_names_and_choose_it_only_on_enter(string runtime)
        {
            // Arrange — the select focused on Open
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "select");
            await using var _ = context;
            var trigger = page.Locator("#check");
            await ComponentChecks.WaitForInputScopeAsync(page, "#check");
            await trigger.FocusAsync();

            // Act — "c" opens the list on Closed, and "ca" moves on to Cancelled
            await ComponentChecks.TypeAsync(page, runtime, "ca");

            // Assert — the focus on Cancelled, nothing chosen until Enter
            var cancelled = page.GetByRole(AriaRole.Option, new() { Name = "Cancelled", Exact = true });
            await Assertions.Expect(cancelled).ToBeFocusedAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("1");
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("3");
            await Assertions.Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_go_on_through_a_space_a_typed_label_holds(string runtime)
        {
            // Arrange — the select focused on Open
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "select");
            await using var _ = context;
            var trigger = page.Locator("#check");
            await ComponentChecks.WaitForInputScopeAsync(page, "#check");
            await trigger.FocusAsync();

            // Act — "on" names Online, and the space typed after it moves on to On Hold rather than choosing Online
            await ComponentChecks.TypeAsync(page, runtime, "on h");

            // Assert — the focus on On Hold, nothing chosen until Enter
            var onHold = page.GetByRole(AriaRole.Option, new() { Name = "On Hold", Exact = true });
            await Assertions.Expect(onHold).ToBeFocusedAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("1");
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("6");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.StampedTypingRuntimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_go_on_through_a_typed_label_whose_keys_reach_a_busy_page_late(string runtime)
        {
            // Arrange — the select focused on Open, on a page the first key typed holds busy past the type-ahead's pause
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "select", page => page.AddInitScriptAsync(HoldingFirstKey));
            await using var _ = context;
            var trigger = page.Locator("#check");
            await ComponentChecks.WaitForInputScopeAsync(page, "#check");
            await trigger.FocusAsync();

            // Act — "on h" typed at a typist's pace, the keys after the "o" reaching the page only once the hold ends
            await ComponentChecks.TypeAsync(page, runtime, "on h");

            // Assert — the label read from when its keys were pressed, the focus on On Hold, nothing chosen until Enter
            var onHold = page.GetByRole(AriaRole.Option, new() { Name = "On Hold", Exact = true });
            await ComponentChecks.ExpectFocusedAsync(page, onHold);
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("1");
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("6");
        }
    }

    public sealed class MultiSelect(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_choose_two_options_from_the_list_it_keeps_open(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "multi-select");
            await using var _ = context;

            // Act
            await page.Locator("#check").ClickAsync();
            await page.GetByRole(AriaRole.Checkbox, new() { Name = "Red", Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Checkbox, new() { Name = "Blue", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("red, blue");
            await Assertions.Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        }
    }

    public sealed class Calendar(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_choose_the_day_pressed(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "calendar");
            await using var _ = context;
            var day = GetDay(page.Locator(".forma-calendar"), 20);

            // Act
            await day.ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2026-03-20");
            await Assertions.Expect(day).ToHaveAttributeAsync("aria-pressed", "true");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_the_day_the_keyboard_reaches_as_one_it_can_choose(string runtime)
        {
            // Arrange — a press puts the focus on a day without showing it, and Tab then moves it on by the keyboard
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "calendar");
            await using var _ = context;
            await GetDay(page.Locator(".forma-calendar"), 20).ClickAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2026-03-20");

            // Act
            await page.Keyboard.PressAsync("Tab");

            // Assert
            var focused = page.Locator(".forma-calendar-day:focus-visible");
            await Assertions.Expect(focused).ToHaveTextAsync("21");
            await Assertions.Expect(focused).ToHaveCSSAsync("opacity", "1");
        }
    }

    public sealed class DateBox(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_choose_a_day_from_the_calendar_it_opens(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "date-box");
            await using var _ = context;

            // Act
            await page.Locator("#check").ClickAsync();
            await GetDay(page.Locator(".forma-calendar"), 20).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2026-03-20");
            await Assertions.Expect(page.Locator(".forma-calendar")).ToBeHiddenAsync();
        }
    }

    public sealed class DatePicker(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_a_day_chosen_from_the_calendar_it_opens(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "date-picker");
            await using var _ = context;

            // Act
            await page.Locator("#check").ClickAsync();
            await GetDay(page.Locator(".forma-calendar"), 20).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2026-03-20");
            await Assertions.Expect(page.Locator(".forma-calendar")).ToBeHiddenAsync();
        }
    }

    public sealed class DateRangePicker(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_the_range_between_two_days_chosen_in_it(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "date-range-picker");
            await using var _ = context;

            // Act
            await page.Locator("#check").ClickAsync();

            // The months open on the bound range's first and last, March and April.
            await GetDay(page.Locator(".forma-calendar-month").First, 20).ClickAsync();
            await GetDay(page.Locator(".forma-calendar-month").Last, 10).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2026-03-20/2026-04-10");
            await Assertions.Expect(page.Locator(".forma-calendar")).ToBeHiddenAsync();
        }
    }

    public sealed class ComboBox(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_its_list_at_least_as_wide_as_its_field(string runtime)
        {
            // Arrange — a field wider than the one option its text leaves
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "combo-box");
            await using var _ = context;
            var field = page.Locator("#check");
            await ComponentChecks.WaitForInteractionAsync(page, "#check", "edits");
            await field.EvaluateAsync("field => field.closest('.forma-combo-box').style.inlineSize = '30rem'");

            // Act
            await field.FillAsync("bogota");
            await Assertions.Expect(page.Locator("#check-list [role=option]")).ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("#check-list")).ToBeVisibleAsync();

            // Assert — the list reads as the field's own, at least the field's width
            var fieldWidth = await field.EvaluateAsync<double>("field => field.closest('.forma-combo-box').getBoundingClientRect().width");
            var listWidth = await page.Locator("#check-list").EvaluateAsync<double>("list => list.getBoundingClientRect().width");
            Assert.True(listWidth >= fieldWidth - 1, $"The list is {listWidth}px wide under a field {fieldWidth}px wide.");
        }
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_narrow_its_list_to_the_text_typed_and_commit_the_active_option_only_on_enter(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "combo-box");
            await using var _ = context;
            var field = page.Locator("#check");
            await ComponentChecks.WaitForInteractionAsync(page, "#check", "edits");

            // Act — "bogota", without its accent, leaves Bogotá alone in the list, and active
            await field.FillAsync("bogota");
            await ComponentChecks.WaitForInputRouteAsync(page, "#check", "combobox.choose");

            // Assert — the field names an option that is drawn, and nothing is committed until Enter
            await Assertions.Expect(page.Locator("#check-list [role=option]")).ToHaveCountAsync(1);
            var activeId = await field.GetAttributeAsync("aria-activedescendant");
            await Assertions.Expect(page.Locator($"#{activeId}")).ToHaveTextAsync("(UTC−05:00) Bogotá");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2");
            await field.PressAsync("Enter");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("3");
            await Assertions.Expect(page.Locator("#check-list")).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_commit_on_leaving_text_that_names_one_option_exactly(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "combo-box");
            await using var _ = context;
            var field = page.Locator("#check");
            await ComponentChecks.WaitForInteractionAsync(page, "#check", "edits");

            // Act
            await field.FillAsync("(utc+01:00) BERLIN");
            await field.PressAsync("Tab");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("4");
            await Assertions.Expect(field).ToHaveValueAsync("(UTC+01:00) Berlin");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_add_a_name_its_list_lacks_and_commit_it(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "combo-box");
            await using var _ = context;
            var field = page.Locator("#team");
            await ComponentChecks.WaitForInteractionAsync(page, "#team", "edits");

            // Act — the list offers only to add Getafe, which Enter takes
            await field.FillAsync("Getafe");
            await ComponentChecks.WaitForInputRouteAsync(page, "#team", "combobox.choose");
            await Assertions.Expect(page.Locator("#team-list [role=option]")).ToHaveTextAsync("Add \"Getafe\"");
            await field.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#team-result")).ToHaveTextAsync("3");
            await Assertions.Expect(field).ToHaveValueAsync("Getafe");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_leave_an_enter_that_ends_a_composition_to_the_input_method(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "combo-box");
            await using var _ = context;
            var field = page.Locator("#check");
            await ComponentChecks.WaitForInteractionAsync(page, "#check", "edits");
            await field.FillAsync("bogota");
            await ComponentChecks.WaitForInputRouteAsync(page, "#check", "combobox.choose");

            // Act — the Enter an input method sends to end its composition
            await field.EvaluateAsync("field => field.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', isComposing: true, bubbles: true, cancelable: true }))");

            // Assert — nothing committed, and the list still open
            await ComponentChecks.MakeRoundTripAsync(page);
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2");
            await Assertions.Expect(page.Locator("#check-list")).ToBeVisibleAsync();
        }
    }

    public sealed class TimeBox(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_commit_a_time_typed_into_it(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "time-box");
            await using var _ = context;
            await ComponentChecks.WaitForInteractionAsync(page, "#check", "edits");

            // Act
            await page.Locator("#check").FillAsync("14:45");
            await page.Locator("#check").PressAsync("Tab");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("14:45");
        }
    }

    public sealed class TimePicker(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_a_time_chosen_from_the_list_it_opens(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "time-picker");
            await using var _ = context;

            // Act
            await page.Locator(".forma-time-box-toggle button").ClickAsync();

            // An option's text is the culture's time, so it is found by its place: every half hour from midnight puts 10:00 at 20.
            await page.Locator("#check-option-20").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("10:00");
            await Assertions.Expect(page.Locator("#check-list")).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_on_its_time_and_bring_the_time_the_arrow_keys_reach_into_view(string runtime)
        {
            // Arrange — the list opened from the field on 09:30, the twentieth half hour from midnight
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "time-picker");
            await using var _ = context;
            var field = page.Locator("#check");
            await ComponentChecks.WaitForInputRouteAsync(page, "#check", "time-box.next");
            await field.FocusAsync();
            await field.PressAsync("ArrowDown");
            await Assertions.Expect(field).ToHaveAttributeAsync("aria-activedescendant", "check-option-19");
            await ComponentChecks.WaitForInViewportAsync(page.Locator("#check-option-19"));

            // Act — six half hours on, to 12:30, past the options the list first drew
            for (var step = 0; step < 6; step++)
            {
                await field.PressAsync("ArrowDown");
            }

            // Assert
            await Assertions.Expect(field).ToHaveAttributeAsync("aria-activedescendant", "check-option-25");
            await ComponentChecks.WaitForInViewportAsync(page.Locator("#check-option-25"));
            await field.PressAsync("Enter");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("12:30");
            await Assertions.Expect(page.Locator("#check-list")).ToBeHiddenAsync();
        }
    }

    public sealed class DateTimePicker(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_a_day_and_a_time_chosen_in_it(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "date-time-picker");
            await using var _ = context;

            // Act
            await page.Locator("#check-date").ClickAsync();
            await GetDay(page.Locator(".forma-calendar"), 20).ClickAsync();
            await page.Locator(".forma-time-box-toggle button").ClickAsync();

            // Every half hour from midnight puts 10:00 at 20.
            await page.Locator("#check-time-option-20").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2026-03-20 10:00");
            await Assertions.Expect(page.Locator(".forma-calendar")).ToBeHiddenAsync();
            await Assertions.Expect(page.Locator("#check-time-list")).ToBeHiddenAsync();
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_report_the_half_left_missing_once_the_reader_leaves_the_field(string runtime)
        {
            // Arrange — the time cleared, which leaves the field waiting for it while the reader is within
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "date-time-picker");
            await using var _ = context;
            var message = page.GetByText("The Start field needs a time.", new() { Exact = true });
            await ComponentChecks.WaitForObservationAsync(page, "#check", "focus");
            await ComponentChecks.WaitForInteractionAsync(page, "#check-time", "edits");
            await page.Locator("#check-time").FillAsync(string.Empty);
            await page.Locator("#check-date").FocusAsync();
            await ComponentChecks.MakeRoundTripAsync(page);
            await Assertions.Expect(message).ToHaveCountAsync(0);

            // Act
            await page.Locator("#outside").FocusAsync();

            // Assert
            await Assertions.Expect(message).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2026-03-14 09:30");
        }
    }

    // A day found by its number, which every culture writes alike, where its accessible name is the culture's long date.
    private static ILocator GetDay(ILocator calendar, int day)
        => calendar.GetByText(day.ToString(CultureInfo.InvariantCulture), new() { Exact = true });
}
