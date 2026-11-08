namespace Forma.Blazor.Browser.Tests;

using static Forma.Blazor.Browser.Tests.CulturePages;

/// <summary>The example hosts, built from Forma's packages as the library's README sets a host up, each starting and switching as the README says.</summary>
public sealed class ExampleHostTests(BrowserHost host)
{
    private const string Welcome = "ברוכים הבאים";
    private const string PaginationLabel = "ניווט עמודים";
    private const string OverriddenPaginationLabel = "ניווט בין העמודים";

    [Theory]
    [InlineData("webassembly")]
    [InlineData("server")]
    public async Task Should_follow_a_switch_from_either_runtime_in_both_and_the_document_under_coordinated_pages(string switching)
    {
        // Arrange
        var (context, page) = await OpenAsync(await host.GetCoordinatedExampleAddressAsync());
        await using var _ = context;

        // Act
        await page.Locator($"#{switching} .switch").ClickAsync();

        // Assert — the client's override of the library's label, which the server reads from the client too
        await ExpectHebrewAsync(page, "server", OverriddenPaginationLabel);
        await ExpectHebrewAsync(page, "webassembly", OverriddenPaginationLabel);
        await WaitForDocumentAsync(page, "he/rtl");
    }

    [Fact]
    public async Task Should_switch_each_runtime_alone_under_an_independent_client_culture()
    {
        // Arrange
        var (context, page) = await OpenAsync(await host.GetIndependentExampleAddressAsync());
        await using var _ = context;
        var served = await GetDocumentAsync(page);

        // Act — the server's island turns its page once the client's switch is done, a round trip on its circuit, which a switch reaching
        // the server or the document from the client would have come through ahead of
        await page.Locator("#webassembly .switch").ClickAsync();
        await ExpectHebrewAsync(page, "webassembly", PaginationLabel);
        await GetNextPage(page, "server").ClickAsync();
        await Assertions.Expect(GetNextPage(page, "server")).ToBeDisabledAsync();
        var afterClient = await GetDocumentAsync(page);
        var serverAfterClient = await page.Locator("#server .welcome").TextContentAsync();
        await page.Locator("#server .switch").ClickAsync();

        // Assert — the client's switch left the server's island and the document as they were
        await ExpectHebrewAsync(page, "server", PaginationLabel);
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

    // The client's own string, from beside its component, and the library's label, from the Hebrew pack or a host's override of it.
    private static async Task ExpectHebrewAsync(IPage page, string island, string paginationLabel)
    {
        await Assertions.Expect(page.Locator($"#{island} .welcome")).ToHaveTextAsync(Welcome);
        await Assertions.Expect(page.Locator($"#{island} nav")).ToHaveAttributeAsync("aria-label", paginationLabel);
    }

    // The island's step to the next of its pagination's two pages that its width shows, the steps' own or the input row's.
    private static ILocator GetNextPage(IPage page, string island)
        => page.Locator($"#{island} .forma-pagination-navigation-button-next:visible");
}
