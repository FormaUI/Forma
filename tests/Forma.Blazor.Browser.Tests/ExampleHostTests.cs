namespace Forma.Blazor.Browser.Tests;

using System.Text.RegularExpressions;

using static Forma.Blazor.Browser.Tests.CulturePages;

/// <summary>The example hosts, built from Forma's packages as the library's README sets a host up, each starting and switching as the README says, and the README's setup held to the examples' own files.</summary>
public sealed partial class ExampleHostTests(BrowserHost host)
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

    [Fact]
    public void Should_set_up_each_example_as_the_readme_says()
    {
        // Arrange
        var readme = File.ReadAllText(Path.Combine(host.RepositoryRoot, "README.md"));

        // Act
        var blocks = ExampleBlock().Matches(readme);

        // Assert — each block's lines appear in its file, in order, MyApp standing for the name of the example whose folder holds the file
        Assert.NotEmpty(blocks);
        Assert.All(blocks, block =>
        {
            var path = block.Groups["path"].Value;
            var name = path.Split('/')[2].Replace(".Client", string.Empty, StringComparison.Ordinal);
            var lines = GetLines(block.Groups["code"].Value.Replace("MyApp", name, StringComparison.Ordinal));
            var file = GetLines(File.ReadAllText(Path.Combine(host.RepositoryRoot, path)));
            var next = 0;

            foreach (var line in lines)
            {
                next = file.IndexOf(line, next) + 1;
                Assert.True(next > 0, $"{path} holds no line '{line}' where the README's block puts it.");
            }
        });
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

    private static List<string> GetLines(string text)
        => [.. text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0)];

    [GeneratedRegex(@"<!-- example: (?<path>[^ ]+) -->\s*```\w*\n(?<code>.*?)```", RegexOptions.Singleline)]
    private static partial Regex ExampleBlock();
}
