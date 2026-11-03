namespace Forma.Blazor.Browser.Tests;

using static Forma.Blazor.Browser.Tests.CulturePages;

/// <summary>The example hosts, built from Forma's packages as the library's README sets a host up, each starting and switching as the README says.</summary>
public sealed class ExampleHostTests(BrowserHost host)
{
    private const string Welcome = "ברוכים הבאים";
    private const string PaginationLabel = "ניווט עמודים";

    [Theory]
    [InlineData("webassembly")]
    [InlineData("server")]
    public async Task Should_follow_a_switch_from_either_runtime_in_both_and_the_document_under_coordinated_pages(string switching)
    {
        // Arrange
        var (context, page) = await OpenAsync(host.CoordinatedExampleAddress);
        await using var _ = context;

        // Act
        await page.Locator($"#{switching} .switch").ClickAsync();

        // Assert
        await ExpectHebrewAsync(page, "server");
        await ExpectHebrewAsync(page, "webassembly");
        await WaitForDocumentAsync(page, "he/rtl");
    }

    [Fact]
    public async Task Should_switch_each_runtime_alone_under_an_independent_client_culture()
    {
        // Arrange
        var (context, page) = await OpenAsync(host.IndependentExampleAddress);
        await using var _ = context;
        var served = await GetDocumentAsync(page);

        // Act
        await page.Locator("#webassembly .switch").ClickAsync();
        await ExpectHebrewAsync(page, "webassembly");
        var afterClient = await GetDocumentAsync(page);
        var serverAfterClient = await page.Locator("#server .welcome").TextContentAsync();
        await page.Locator("#server .switch").ClickAsync();

        // Assert — the client's switch left the server's island and the document as they were
        await ExpectHebrewAsync(page, "server");
        await WaitForDocumentAsync(page, "he/rtl");
        Assert.Multiple(
            () => Assert.Equal(served, afterClient),
            () => Assert.Equal("Welcome", serverAfterClient));
    }

    private async Task<(IBrowserContext Context, IPage Page)> OpenAsync(Uri address)
    {
        var (context, page) = await host.OpenPageAsync(address);
        await page.GotoAsync(string.Empty);
        await Assertions.Expect(page.Locator("#server .runtime")).ToHaveTextAsync("Server True", new() { Timeout = 60000 });
        await Assertions.Expect(page.Locator("#webassembly .runtime")).ToHaveTextAsync("WebAssembly True", new() { Timeout = 60000 });
        await Assertions.Expect(page.Locator("#server .welcome")).ToHaveTextAsync("Welcome");
        await Assertions.Expect(page.Locator("#webassembly .welcome")).ToHaveTextAsync("Welcome");

        return (context, page);
    }

    // The host's own string, from its folder, and the library's, from the Hebrew pack.
    private static async Task ExpectHebrewAsync(IPage page, string island)
    {
        await Assertions.Expect(page.Locator($"#{island} .welcome")).ToHaveTextAsync(Welcome);
        await Assertions.Expect(page.Locator($"#{island} nav")).ToHaveAttributeAsync("aria-label", PaginationLabel);
    }
}
