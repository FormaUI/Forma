namespace Forma.Blazor.Browser.Tests.Components;

using System.Text.RegularExpressions;

/// <summary>The navigation components' interactions, in every runtime.</summary>
public sealed partial class NavigationTests
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

    public sealed class LinkTabs(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_go_to_the_page_of_the_tab_pressed_and_mark_that_tab_current(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "link-tabs");
            await using var _ = context;
            var tabs = page.Locator("#link-tabs");
            var membersPath = "link-tabs/members";
            await Assertions.Expect(tabs.Locator("[aria-current=page]")).ToHaveTextAsync("Overview");

            // Act
            await tabs.GetByRole(AriaRole.Link, new() { Name = "Members", Exact = true }).ClickAsync();

            // Assert — the narrow strip follows too, bringing its newly current tab into view
            await Assertions.Expect(page).ToHaveURLAsync(new Regex($"{Regex.Escape(membersPath)}$"));
            await Assertions.Expect(tabs.Locator("[aria-current=page]")).ToHaveTextAsync("Members");
            await Assertions.Expect(page.Locator("#narrow-link-tabs [aria-current=page]")).ToBeInViewportAsync(new() { Ratio = 1 });
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_mark_the_tab_of_the_page_it_opens_on_beneath_none_of_the_others(string runtime)
        {
            // Act
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "link-tabs/settings");
            await using var _ = context;

            // Assert — the overview matches only its own address, so a page beneath it leaves it unmarked, and the narrow strip scrolls
            // its last tab into view
            await Assertions.Expect(page.Locator("#link-tabs [aria-current=page]")).ToHaveTextAsync("Settings");
            await Assertions.Expect(page.Locator("#link-tabs [aria-current]")).ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("#narrow-link-tabs [aria-current=page]")).ToBeInViewportAsync(new() { Ratio = 1 });
        }
    }

    public sealed partial class Stepper(BrowserHost host)
    {
        // How far a stepper's steps reach past it, and each step: its name, whether its name is drawn, how far its top stands from
        // the first step's, and whether it is the current step.
        private const string ReadSteps = """
            box => {
                const stepper = document.querySelector(`#${box} .forma-stepper-list`);
                const first = stepper.querySelector('.forma-stepper-marker').getBoundingClientRect();
                return [
                    `spill ${Math.round(stepper.scrollWidth - stepper.clientWidth)}`,
                    ...[...stepper.querySelectorAll('li')].map(step => {
                        const name = step.querySelector('.forma-stepper-label');
                        const marker = step.querySelector('.forma-stepper-marker').getBoundingClientRect();
                        const shown = name.getBoundingClientRect().width > 1 ? 'named' : 'numbered';
                        return `${name.textContent} ${shown} ${Math.round(marker.top - first.top)}${step.getAttribute('aria-current') === 'step' ? ' current' : ''}`;
                    })
                ];
            }
            """;

        [GeneratedRegex(@"\bis-compact\b")]
        private static partial Regex IsCompact();

        // Whether the narrow stepper's link to its completed step, shown by its number alone, takes the least size a target takes under the
        // page's pointer, a finger's where the pointer is coarse.
        private const string ReadNumberedLink = """
            () => {
                const box = document.querySelector('#narrow .forma-stepper-list a.forma-stepper-target').getBoundingClientRect();
                const least = matchMedia('(pointer: coarse)').matches ? 44 : 24;
                return `${box.width >= least && box.height >= least ? 'takes' : 'misses'} ${least}: ${Math.round(box.width)} by ${Math.round(box.height)}`;
            }
            """;

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_name_every_step_where_its_names_fit_and_number_all_but_the_current_one_where_they_do_not(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "stepper");
            await using var _ = context;

            // Act — once the narrow stepper has measured that its names don't fit
            await Assertions.Expect(page.Locator("#narrow .forma-stepper")).ToHaveClassAsync(IsCompact());
            var wide = await page.EvaluateAsync<string[]>(ReadSteps, "wide");
            var narrow = await page.EvaluateAsync<string[]>(ReadSteps, "narrow");

            // Assert — one row in both, every name on a page's width and the current step's alone at a phone's narrowest
            Assert.Multiple(
                () => Assert.Equal(["spill 0", "Find your event named 0", "Choose your photos named 0 current", "Pay named 0", "Download named 0"], wide),
                () => Assert.Equal(["spill 0", "Find your event numbered 0", "Choose your photos named 0 current", "Pay numbered 0", "Download numbered 0"], narrow));
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_name_every_step_again_once_given_the_room_and_number_them_again_once_it_is_taken_away(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "stepper");
            await using var _ = context;
            var narrow = page.Locator("#narrow");
            var stepper = narrow.Locator(".forma-stepper");
            await Assertions.Expect(stepper).ToHaveClassAsync(IsCompact());

            // Act
            await narrow.EvaluateAsync("box => box.style.inlineSize = '60rem'");
            await Assertions.Expect(stepper).Not.ToHaveClassAsync(IsCompact());
            var widened = await page.EvaluateAsync<string[]>(ReadSteps, "narrow");
            await narrow.EvaluateAsync("box => box.style.inlineSize = '20rem'");

            // Assert
            Assert.Equal(["spill 0", "Find your event named 0", "Choose your photos named 0 current", "Pay named 0", "Download named 0"], widened);
            await Assertions.Expect(stepper).ToHaveClassAsync(IsCompact());
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_give_a_link_shown_by_its_number_alone_a_whole_target(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "stepper");
            await using var _ = context;
            await Assertions.Expect(page.Locator("#narrow .forma-stepper")).ToHaveClassAsync(IsCompact());

            // Act
            var target = await page.EvaluateAsync<string>(ReadNumberedLink);

            // Assert
            Assert.StartsWith("takes ", target, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_name_its_steps_and_link_a_completed_one_back(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "stepper");
            await using var _ = context;
            var stepper = page.GetByRole(AriaRole.List, new() { Name = "Buying photos", Exact = true });

            // Assert
            await Assertions.Expect(stepper.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(4);
            await Assertions.Expect(stepper.Locator("[aria-current=step]")).ToContainTextAsync("Choose your photos");
            await Assertions.Expect(stepper.GetByRole(AriaRole.Link)).ToHaveAccessibleNameAsync("Find your event Completed");
            await Assertions.Expect(stepper.GetByRole(AriaRole.Link)).ToHaveCountAsync(1);
        }
    }

    public sealed class CommandPalette(BrowserHost host)
    {
        // Whether the palette fills the viewport, once its opening has settled, and whether the viewport is a phone's.
        private const string ReadFill = """
            () => {
                const box = document.querySelector('#palette').getBoundingClientRect();
                const isPhone = matchMedia('(max-width: 40rem)').matches;
                const fills = Math.round(box.width) >= innerWidth - 1 && Math.round(box.height) >= innerHeight - 1;
                return `${isPhone ? 'phone' : 'wider'} ${fills ? 'fills' : 'leaves room'}`;
            }
            """;

        private const string IsSettled = """
            () => ['none', 'matrix(1, 0, 0, 1, 0, 0)'].includes(getComputedStyle(document.querySelector('#palette')).transform)
            """;

        private static ILocator GetField(IPage page)
            => page.GetByRole(AriaRole.Combobox, new() { Name = "Search Commands", Exact = true });

        // Opens the palette from the keyboard, as a press focuses a button in some engines and not in WebKit, so the opener holds the
        // focus the palette hands back as it closes.
        private static async Task OpenFromAsync(IPage page, ILocator opener)
        {
            await opener.FocusAsync();
            await page.Keyboard.PressAsync("Enter");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_search_move_and_run_an_action_closing_first_and_returning_the_focus_to_its_opener(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "command-palette");
            await using var _ = context;
            var opener = page.Locator("#open");
            var field = GetField(page);
            var options = page.GetByRole(AriaRole.Option);
            await OpenFromAsync(page, opener);
            await Assertions.Expect(field).ToBeFocusedAsync();

            // Act
            await field.PressSequentiallyAsync("ord");
            await Assertions.Expect(options).ToHaveCountAsync(2);
            await page.Keyboard.PressAsync("ArrowDown");
            await Assertions.Expect(field).ToHaveAttributeAsync("aria-activedescendant", (await options.Nth(1).GetAttributeAsync("id"))!);
            await page.Keyboard.PressAsync("Enter");

            // Assert — the name starting with the query first, the action run, the palette closed and the focus back on its opener
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("refreshed");
            await Assertions.Expect(page.Locator("#palette")).ToBeHiddenAsync();
            await Assertions.Expect(opener).ToBeFocusedAsync();
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_list_a_name_starting_with_the_query_first_and_close_on_escape(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "command-palette");
            await using var _ = context;
            var opener = page.Locator("#open");
            var field = GetField(page);
            await OpenFromAsync(page, opener);
            await field.PressSequentiallyAsync("ord");

            await Assertions.Expect(page.Locator(".forma-command-palette-name")).ToHaveTextAsync(["Orders", "Refresh the Orders"]);

            // Act
            await page.Keyboard.PressAsync("Escape");

            // Assert
            await Assertions.Expect(page.Locator("#palette")).ToBeHiddenAsync();
            await Assertions.Expect(opener).ToBeFocusedAsync();
            Assert.Equal("none", await page.Locator("#result").TextContentAsync());
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_fill_the_viewport_where_it_is_a_phone_s_and_leave_room_around_it_elsewhere(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "command-palette");
            await using var _ = context;
            await page.Locator("#open").ClickAsync();
            await Assertions.Expect(GetField(page)).ToBeFocusedAsync();
            await page.WaitForFunctionAsync(IsSettled);

            // Act
            var fill = await page.EvaluateAsync<string>(ReadFill);

            // Assert
            Assert.Contains(fill, (string[])["phone fills", "wider leaves room"]);
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
            await ComponentChecks.WaitForInteractionAsync(page, "#entry .forma-pagination-navigation-input-box", "edits");

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
            await ComponentChecks.WaitForInteractionAsync(page, "#entry .forma-pagination-navigation-input-box", "edits");
            await entry.FillAsync(string.Empty);
            await entry.EvaluateAsync("entry => entry.value = '9'");

            // Act
            await entry.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#entry-result")).ToHaveTextAsync("9");
        }
    }
}
