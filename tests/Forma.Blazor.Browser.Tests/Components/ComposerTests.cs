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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_link_the_text_selected_through_the_link_dialog(string runtime)
        {
            // Arrange — the whole text selected and the link dialog opened on it
            var (context, page) = await OpenAsync(host, runtime, "composer");
            await using var _ = context;
            var canvas = page.Locator("#check .forma-composer-rich-text-canvas");
            await WaitForInputScopeAsync(page, "#check .forma-composer-rich-text-canvas");
            await canvas.PressSequentiallyAsync("Docs");
            await canvas.PressAsync("ControlOrMeta+A");
            await page.WaitForFunctionAsync("() => getSelection().toString() === 'Docs'");
            await page.Locator("#check .forma-composer-toolbar").GetByRole(AriaRole.Button, new() { Name = "Insert Link", Exact = true }).ClickAsync();
            var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Insert Link" });


            // Act
            await dialog.GetByLabel("URL").FillAsync("https://example.com/docs");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(canvas.Locator("a[href='https://example.com/docs']")).ToHaveTextAsync("Docs");
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("href=\"https://example.com/docs\"");
        }
    }
}
