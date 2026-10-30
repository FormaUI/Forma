namespace Forma.Blazor.Browser.Tests.Components;

using System.Globalization;

/// <summary>The selects', the calendar's and the date and time pickers' main interactions, in every runtime.</summary>
public sealed class PickersTests
{
    public sealed class Select(BrowserHost host)
    {
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

    public sealed class TimeBox(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_commit_a_time_typed_into_it(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "time-box");
            await using var _ = context;

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
    }

    // A day found by its number, which every culture writes alike, where its accessible name is the culture's long date.
    private static ILocator GetDay(ILocator calendar, int day)
        => calendar.GetByText(day.ToString(CultureInfo.InvariantCulture), new() { Exact = true });
}
