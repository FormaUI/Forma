namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The virtualization's interactions, in every runtime.</summary>
public sealed class VirtualizationTests
{
    public sealed class Virtualize(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_the_items_scrolled_to_in_place_of_those_scrolled_away(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "virtualize");
            await using var _ = context;
            var first = page.Locator("#check").GetByText("Item 0", new() { Exact = true });
            var middle = page.Locator("#check").GetByText("Item 500", new() { Exact = true });
            await Assertions.Expect(first).ToBeVisibleAsync();
            await Assertions.Expect(middle).ToHaveCountAsync(0);

            // Act
            await page.Locator("#check").EvaluateAsync("box => box.scrollTop = 500 * 40");

            // Assert
            await ComponentChecks.WaitForInViewportAsync(middle);
            await Assertions.Expect(first).ToHaveCountAsync(0);
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_on_the_item_it_is_given_and_scroll_to_the_items_asked_for(string runtime)
        {
            // Arrange — the list opened on its three hundredth item
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "virtualize");
            await using var _ = context;
            var box = page.Locator("#opened");
            await ComponentChecks.WaitForInViewportAsync(page.Locator("#opened-300"));

            // Act — an item far past those drawn, then one already drawn
            await page.Locator("#to-700").ClickAsync();
            await page.WaitForFunctionAsync(IsAtTop, new object[] { await box.ElementHandleAsync(), "opened-700" });
            await page.Locator("#to-302").ClickAsync();

            // Assert
            await page.WaitForFunctionAsync(IsAtTop, new object[] { await box.ElementHandleAsync(), "opened-302" });
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_keep_the_focused_item_s_element_and_its_focus_while_it_scrolls_away_and_back(string runtime)
        {
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "virtualize");
            await using var _ = context;

            // A list of divs, then the rows of a table
            foreach (var (box, checkbox, neighbor) in new[] { ("#pinned", "#pinned-2", "Item 3"), ("#pinned-rows", "#row-2", "Row 3") })
            {
                // Arrange — the third item's checkbox focused, which pins its item
                var list = page.Locator(box);
                var beside = list.GetByText(neighbor, new() { Exact = true });
                await page.Locator(checkbox).FocusAsync();
                await ComponentChecks.MakeRoundTripAsync(page);
                var focused = await page.Locator(checkbox).ElementHandleAsync();

                // Act — scrolled far enough that the item beside it leaves the rendered range, and back until that item shows again
                await list.EvaluateAsync("box => box.scrollTop = box.scrollHeight / 2");
                await Assertions.Expect(beside).ToHaveCountAsync(0, new() { Timeout = 30000 });
                var isHeldAway = await focused.EvaluateAsync<bool>(IsConnectedAndFocused);
                await list.EvaluateAsync("box => box.scrollTop = 0");
                await ComponentChecks.WaitForInViewportAsync(beside);
                var isHeldBack = await focused.EvaluateAsync<bool>(IsConnectedAndFocused);

                // Assert
                Assert.Multiple(
                    () => Assert.True(isHeldAway, $"{checkbox} lost its element or its focus as {box} scrolled away from it."),
                    () => Assert.True(isHeldBack, $"{checkbox} lost its element or its focus as {box} scrolled back to it."));
            }
        }

        // Whether the element is still on the page and still holds the focus.
        private const string IsConnectedAndFocused = "element => element.isConnected && document.activeElement === element";

        // Whether the item named by its id stands at the top of the box's viewport.
        private const string IsAtTop = "([box, id]) => { const item = document.getElementById(id); return item !== null && Math.abs(item.getBoundingClientRect().top - box.getBoundingClientRect().top) <= 1; }";
    }
}
