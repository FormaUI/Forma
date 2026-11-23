namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The charts' readouts from the pointer and from the keyboard, their direction and their fit at a phone's width, in every runtime.</summary>
public sealed class ChartsTests
{
    // The text of every label, legend entry or readout of the chart that reaches past the chart's own box, which a chart given its
    // width keeps within it.
    private const string ReadOverflow = """
        id => {
            const chart = document.getElementById(id);
            const bounds = chart.getBoundingClientRect();
            const parts = [...chart.querySelectorAll('.forma-chart-time-label bdi, .forma-chart-value-label, .forma-chart-threshold-label, .forma-chart-legend-item, .forma-chart-readout')]
                .filter(part => part.getClientRects().length > 0)
                .filter(part => { const box = part.getBoundingClientRect(); return box.left < bounds.left - 1 || box.right > bounds.right + 1; })
                .map(part => part.textContent.trim());
            return chart.scrollWidth > chart.clientWidth + 1 ? [`scrolls ${chart.scrollWidth - chart.clientWidth}px`, ...parts] : parts;
        }
        """;

    public sealed class LineChart(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_the_values_of_the_period_under_the_pointer(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "line-chart");
            await using var _ = context;
            var period = page.Locator("#check .forma-chart-period").Nth(20);

            // Act
            await period.HoverAsync();

            // Assert — the twenty-first hour, its median 300 ms and its 95th percentile 500 ms
            var readout = period.Locator(".forma-chart-readout");
            await Assertions.Expect(readout).ToBeVisibleAsync();
            await Assertions.Expect(readout).ToContainTextAsync("300 ms");
            await Assertions.Expect(readout).ToContainTextAsync("500 ms");
            await Assertions.Expect(page.Locator("#check .forma-chart-readout:visible")).ToHaveCountAsync(1);
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_read_the_periods_from_the_keyboard_starting_with_the_last(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "line-chart");
            await using var _ = context;
            var control = page.Locator("#check input.forma-chart-scrubber");
            await control.FocusAsync();

            // Act
            await page.Keyboard.PressAsync("ArrowLeft");

            // Assert — the hour before the last, its readout shown and its values said
            var read = page.Locator("#check .forma-chart-period.is-read");
            await Assertions.Expect(control).ToHaveAttributeAsync("aria-valuetext", new System.Text.RegularExpressions.Regex("Median 320 ms, 95th percentile 520 ms$"));
            await Assertions.Expect(read).ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("#check .forma-chart-period").Nth(22)).ToContainClassAsync("is-read");
            await Assertions.Expect(read.Locator(".forma-chart-readout")).ToBeVisibleAsync();
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_run_its_time_from_the_inline_start_in_a_right_to_left_container(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "line-chart");
            await using var _ = context;
            var periods = page.Locator("#rtl-chart .forma-chart-period");

            // Act
            var first = (await periods.First.BoundingBoxAsync())!;
            var last = (await periods.Last.BoundingBoxAsync())!;
            var chart = (await page.Locator("#rtl-chart").BoundingBoxAsync())!;
            var lastMarker = (await page.Locator("#rtl-chart .forma-chart-marker.is-standing").First.BoundingBoxAsync())!;
            var drawing = await page.Locator("#rtl-chart .forma-chart-lines").EvaluateAsync<string>("svg => getComputedStyle(svg).transform");

            // Assert — the latest hour at the left, its marker beside it, and the drawing mirrored to meet it
            Assert.Multiple(
                () => Assert.True(last.X < first.X, $"The last period stands at {last.X}px, right of the first at {first.X}px."),
                () => Assert.True(lastMarker.X < chart.X + (chart.Width / 2), $"The last point's marker stands at {lastMarker.X}px, in the chart's right half."),
                () => Assert.Equal("matrix(-1, 0, 0, 1, 0, 0)", drawing));
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_keep_its_labels_within_a_phone_s_width(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "line-chart");
            await using var _ = context;

            // Act
            await page.SetViewportSizeAsync(360, page.ViewportSize!.Height);
            await page.Locator("#check .forma-chart-period").Nth(20).HoverAsync();

            // Assert
            Assert.Empty(await page.EvaluateAsync<string[]>(ReadOverflow, "check"));
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_read_a_span_s_first_point_with_its_mark_and_note_and_show_its_table_from_its_toggle(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "line-chart");
            await using var _ = context;
            var chart = page.Locator("#span-chart");
            var control = chart.Locator("input.forma-chart-scrubber");
            await control.FocusAsync();

            // Act
            await page.Keyboard.PressAsync("Home");
            await Assertions.Expect(control).ToHaveAttributeAsync("aria-valuetext", new System.Text.RegularExpressions.Regex(@"Value 1,000 \(Deposit: Opened\)$"));
            var readout = chart.Locator(".forma-chart-period").First.Locator(".forma-chart-readout");
            await Assertions.Expect(readout).ToBeVisibleAsync();
            await chart.Locator("summary").ClickAsync();

            // Assert — the marked points drawn in their marks' shapes, the readout naming the mark and the note, and the table open
            await Assertions.Expect(readout.Locator(".forma-chart-readout-note")).ToHaveTextAsync("Deposit: Opened");
            await Assertions.Expect(chart.Locator(".forma-chart-marker.is-marked")).ToHaveCountAsync(2);
            await Assertions.Expect(chart.Locator(".forma-chart-legend-name")).ToHaveTextAsync(["Deposit", "Fee"]);
            await Assertions.Expect(chart.Locator(".forma-chart-table tbody tr")).ToHaveCountAsync(3);
            await Assertions.Expect(chart.Locator(".forma-chart-table")).ToBeVisibleAsync();
        }
    }

    public sealed class BarChart(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_stand_a_period_s_bars_in_its_series_order_and_show_their_values_under_the_pointer(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "bar-chart");
            await using var _ = context;
            var period = page.Locator("#check .forma-chart-period").Nth(28);

            // Act
            await period.HoverAsync();

            // Assert — the 29th day, 29 failures and 14 retries, the failures' bar first
            var failures = (await period.Locator(".forma-chart-bar.forma-chart-series-1").BoundingBoxAsync())!;
            var retries = (await period.Locator(".forma-chart-bar.forma-chart-series-2").BoundingBoxAsync())!;
            var readout = period.Locator(".forma-chart-readout");
            await Assertions.Expect(readout).ToBeVisibleAsync();
            await Assertions.Expect(readout.Locator(".forma-chart-readout-value")).ToHaveTextAsync(["29", "14"]);
            Assert.True(failures.X < retries.X, $"The failures' bar stands at {failures.X}px, after the retries' at {retries.X}px.");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_say_a_period_read_from_the_keyboard_has_no_data_where_it_has_none(string runtime)
        {
            // Arrange — the 24th day, five before the last, holds no failures
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "bar-chart");
            await using var _ = context;
            var control = page.Locator("#check input.forma-chart-scrubber");
            await control.FocusAsync();

            // Act
            for (var step = 0; step < 6; step++)
            {
                await page.Keyboard.PressAsync("ArrowLeft");
            }

            // Assert
            await Assertions.Expect(control).ToHaveAttributeAsync("aria-valuetext", new System.Text.RegularExpressions.Regex("Failures No data, Retries 12$"));
            await Assertions.Expect(page.Locator("#check .forma-chart-period").Nth(23).Locator(".forma-chart-readout")).ToBeVisibleAsync();
            Assert.Empty(await page.EvaluateAsync<string[]>(ReadOverflow, "check"));
        }
    }
}
