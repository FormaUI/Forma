namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The navigation components' interactions, in every runtime.</summary>
public sealed class NavigationTests
{
    public sealed class Breadcrumb(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_report_the_segment_pressed(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "breadcrumb");
            await using var _ = context;
            var breadcrumb = page.Locator("#breadcrumb");

            // Act
            await breadcrumb.GetByRole(AriaRole.Button, new() { Name = "Store", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("store");
            await Assertions.Expect(breadcrumb.Locator("[aria-current=page]")).ToHaveTextAsync("Orders");
        }
    }

    public sealed class Pagination(BrowserHost host)
    {
        // Gives every pager the width, and names each row that reaches past it, by its spill in the row's direction.
        private const string ReadSpills = """
            width => [...document.querySelectorAll('nav.forma-pagination')].flatMap(pager => {
                pager.style.inlineSize = `${width}px`;
                const bounds = pager.getBoundingClientRect();
                const rtl = getComputedStyle(pager).direction === 'rtl';
                return [...pager.children].filter(row => getComputedStyle(row).display !== 'none').flatMap(row => {
                    const parts = [...row.querySelectorAll(':scope > *, :scope > * > *')]
                        .filter(part => getComputedStyle(part).position !== 'absolute' && part.getBoundingClientRect().width > 0);
                    const spill = Math.max(row.scrollWidth - pager.clientWidth, ...parts.map(part => {
                        const box = part.getBoundingClientRect();
                        return Math.round(rtl ? bounds.left - box.left : box.right - bounds.right);
                    }));
                    return spill > 0 ? [`#${pager.id}'s ${row.className.split(' ')[0]} spills ${spill}px`] : [];
                });
            })
            """;

        // Makes each word on the navigation buttons and the Go button half again as long, as a language's words can be.
        private const string LengthenWords = """
            () => {
                const lengthen = text => {
                    const word = text.textContent.trim();
                    text.textContent = word + word.slice(0, Math.ceil(word.length / 2));
                };
                for (const text of document.querySelectorAll('.forma-pagination-navigation-button-text')) lengthen(text);
                for (const go of document.querySelectorAll('.forma-pagination-go-button')) {
                    for (const node of go.childNodes) if (node.nodeType === Node.TEXT_NODE && node.textContent.trim()) lengthen(node);
                }
            }
            """;

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_move_to_the_page_pressed_and_a_run_of_pages_on_next(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "pagination");
            await using var _ = context;
            var steps = page.Locator("#pagination .forma-pagination-navigation-steps");
            var result = page.Locator("#result");
            await Assertions.Expect(result).ToHaveTextAsync("1");

            // Act
            await steps.GetByRole(AriaRole.Button, new() { Name = "Page 3", Exact = true }).ClickAsync();
            await Assertions.Expect(result).ToHaveTextAsync("3");
            await steps.Locator(".forma-pagination-navigation-button-next").ClickAsync();

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("7");
            await Assertions.Expect(steps.Locator("[aria-current=page]")).ToHaveTextAsync("7");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_page_forward_and_back_as_the_wheel_turns_over_it(string runtime)
        {
            // Arrange — the pointer over the pagination, whose wheel script routes a turn as a page
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "pagination");
            await using var _ = context;
            var result = page.Locator("#result");
            await ComponentChecks.WaitForInputRouteAsync(page, "#pagination", "pagination.page");
            await page.Locator("#pagination").HoverAsync();

            // Act
            await page.Mouse.WheelAsync(0, 100);
            await Assertions.Expect(result).ToHaveTextAsync("2");
            await page.Mouse.WheelAsync(0, -100);

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("1");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_select_its_entry_on_focus_and_go_to_the_page_typed_over_it(string runtime)
        {
            // Arrange — the entry focused, its whole text selected so the first key replaces it
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "pagination");
            await using var _ = context;
            var entry = page.Locator("#entry .forma-pagination-navigation-input-box");
            await ComponentChecks.WaitForInputRouteAsync(page, "#entry .forma-pagination-navigation-input-box", "pagination.go");
            await entry.FocusAsync();
            await page.WaitForFunctionAsync("entry => entry.value.length > 0 && entry.selectionStart === 0 && entry.selectionEnd === entry.value.length", await entry.ElementHandleAsync());

            // Act
            await entry.PressSequentiallyAsync("7");
            await entry.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#entry-result")).ToHaveTextAsync("7");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("1");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_stay_within_its_width_on_either_side_of_each_switch_and_with_longer_words(string runtime)
        {
            // Arrange — the widths on either side of where the steps give way to the input row (36.5rem), the words
            // to their glyphs (21.25rem) and the box to its content (17.75rem), and a width below them all
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "pagination");
            await using var _ = context;
            int[] widths = [260, 284, 285, 320, 340, 341, 400, 584, 585, 640];
            var spills = new List<string>();

            // Act — each pager given each width, with its own words and then with words half again as long
            foreach (var words in (string[])["its own words", "words half again as long"])
            {
                if (words != "its own words")
                {
                    await page.EvaluateAsync(LengthenWords);
                }

                foreach (var width in widths)
                {
                    spills.AddRange((await page.EvaluateAsync<string[]>(ReadSpills, width)).Select(spill => $"{spill} at {width}px with {words}"));
                }
            }

            // Assert
            Assert.Empty(spills);
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_go_to_a_page_its_entry_holds_though_no_input_reported_it(string runtime)
        {
            // Arrange — the entry emptied, then given a page with no input event, as text typed before the runtime listens is
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "pagination");
            await using var _ = context;
            var entry = page.Locator("#entry .forma-pagination-navigation-input-box");
            await ComponentChecks.WaitForInputRouteAsync(page, "#entry .forma-pagination-navigation-input-box", "pagination.go");
            await entry.FillAsync(string.Empty);
            await entry.EvaluateAsync("entry => entry.value = '9'");

            // Act
            await entry.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#entry-result")).ToHaveTextAsync("9");
        }
    }
}
