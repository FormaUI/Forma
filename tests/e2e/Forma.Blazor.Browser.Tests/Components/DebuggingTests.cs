namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The debugging components' interactions, in every runtime.</summary>
public sealed class DebuggingTests
{
    public sealed class InputInspector(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_report_the_action_the_router_chose_for_a_key_pressed_in_a_scope(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "input-inspector");
            await using var _ = context;
            await ComponentChecks.WaitForInputRouteAsync(page, "#scope", "check.next");
            await ComponentChecks.WaitForInspectingAsync(page.Locator("#inspector"));
            var inside = page.Locator("#inside");
            await inside.FocusAsync();

            // Act
            await inside.PressAsync("ArrowDown");

            // Assert
            await Assertions.Expect(page.Locator("#inspector .forma-input-inspector-decision code").Filter(new() { HasText = "check.next" })).ToHaveCountAsync(1);
        }
    }
}
