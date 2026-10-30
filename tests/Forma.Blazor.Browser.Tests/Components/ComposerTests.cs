namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The composer's main interaction, in every runtime.</summary>
public sealed class ComposerTests
{
    public sealed class Composer(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_write_the_text_typed_and_made_bold_into_the_bound_document(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "composer");
            await using var _ = context;
            var canvas = page.Locator("#check .forma-composer-rich-text-canvas");
            var bold = page.Locator("#check .forma-composer-toolbar").GetByRole(AriaRole.Button, new() { Name = "Bold", Exact = true });
            await WaitForInputScopeAsync(page, "#check .forma-composer-rich-text-canvas");

            // Act — the whole text selected before Bold is pressed
            await canvas.PressSequentiallyAsync("Hello");
            await canvas.PressAsync("ControlOrMeta+A");
            await page.WaitForFunctionAsync("() => getSelection().toString() === 'Hello'");
            await bold.ClickAsync();

            // Assert
            await Assertions.Expect(canvas.Locator("b")).ToHaveTextAsync("Hello");
            await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
            await Assertions.Expect(page.Locator("#check .forma-composer-rich-text-announcer")).ToHaveTextAsync("Bold on");
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("<b>Hello</b>");
        }
    }
}
