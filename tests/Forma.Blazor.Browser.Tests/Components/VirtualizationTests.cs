namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The virtualization's main interaction, in every runtime.</summary>
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
            var first = page.GetByText("Item 0", new() { Exact = true });
            var middle = page.GetByText("Item 500", new() { Exact = true });
            await Assertions.Expect(first).ToBeVisibleAsync();
            await Assertions.Expect(middle).ToHaveCountAsync(0);

            // Act
            await page.Locator("#check").EvaluateAsync("box => box.scrollTop = 500 * 40");

            // Assert
            await Assertions.Expect(middle).ToBeInViewportAsync();
            await Assertions.Expect(first).ToHaveCountAsync(0);
        }
    }
}
