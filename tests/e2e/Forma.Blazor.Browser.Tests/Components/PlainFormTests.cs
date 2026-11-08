namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>A plain form of Forma's fields under static server rendering, posted as an ordinary web form is.</summary>
public sealed class PlainFormTests(BrowserHost host)
{
    [Fact]
    public async Task Should_post_what_the_reader_entered_in_each_field_for_the_server_to_bind()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(await host.GetComponentsAddressAsync());
        await using var _ = context;
        await page.GotoAsync("static/plain-form");
        await Assertions.Expect(page.Locator("#runtime")).ToHaveTextAsync("Static False");
        await page.Locator("#name").FillAsync("Ada");
        await page.Locator("#secret").FillAsync("s3cret");
        await page.Locator("#notes").FillAsync("Notes on engines");
        await page.Locator("#agreed").CheckAsync();
        await page.Locator("label.forma-toggle-switch").Filter(new() { Has = page.Locator("#subscribed") }).ClickAsync();
        await page.Locator("#volume").FillAsync("7");
        await page.Locator("#color").FillAsync("#336699");

        // Act
        await page.Locator("#post").ClickAsync();

        // Assert
        await Assertions.Expect(page.Locator("#posted")).ToHaveTextAsync("Ada|s3cret|Notes on engines|True|True|7|#336699");
    }
}
