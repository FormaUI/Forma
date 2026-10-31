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
            await Assertions.Expect(middle).ToBeInViewportAsync();
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
            await Assertions.Expect(page.Locator("#opened-300")).ToBeInViewportAsync();

            // Act — an item far past those drawn, then one already drawn
            await page.Locator("#to-700").ClickAsync();
            await page.WaitForFunctionAsync(IsAtTop, new object[] { await box.ElementHandleAsync(), "opened-700" });
            await page.Locator("#to-302").ClickAsync();

            // Assert
            await page.WaitForFunctionAsync(IsAtTop, new object[] { await box.ElementHandleAsync(), "opened-302" });
        }

        // Whether the item named by its id stands at the top of the box's viewport.
        private const string IsAtTop = "([box, id]) => { const item = document.getElementById(id); return item !== null && Math.abs(item.getBoundingClientRect().top - box.getBoundingClientRect().top) <= 1; }";
    }
}
