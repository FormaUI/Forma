namespace Forma.Examples.Smoke.Tests;

/// <summary>The Web App example hosts, built from Forma's packages as the library's README sets a host up: each page's Server island and WebAssembly island becoming interactive and turning a page of the library's pagination.</summary>
public sealed class WebExampleTests(ExampleHosts hosts)
{
    private static readonly string[] Islands = ["server", "webassembly"];

    [Theory]
    [InlineData("Coordinated")]
    [InlineData("Independent")]
    public async Task Should_turn_a_page_of_the_library_s_pagination_in_each_runtime(string example)
    {
        // Arrange
        var (context, page) = await hosts.OpenPageAsync(example);
        await using var _ = context;
        await Assertions.Expect(page.Locator("#server .runtime")).ToHaveTextAsync("Server True", new() { Timeout = 60000 });
        await Assertions.Expect(page.Locator("#webassembly .runtime")).ToHaveTextAsync("WebAssembly True", new() { Timeout = 60000 });

        // Act
        foreach (var island in Islands)
        {
            await GetNextPage(page, island).ClickAsync();
        }

        // Assert — the second of two pages is the last, so the step past it closes
        foreach (var island in Islands)
        {
            await Assertions.Expect(GetNextPage(page, island)).ToBeDisabledAsync();
        }
    }

    // The pagination's step to the next page that its width shows, the steps' own or the input row's.
    private static ILocator GetNextPage(IPage page, string island)
        => page.Locator($"#{island} .forma-pagination-navigation-button-next:visible");
}
