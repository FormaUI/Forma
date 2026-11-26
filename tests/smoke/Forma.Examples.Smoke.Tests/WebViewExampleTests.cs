namespace Forma.Examples.Smoke.Tests;

/// <summary>The Blazor Hybrid example hosts, built from Forma's packages as the library's README sets an application up: each starting from its publish with the library's stylesheets loaded and turning a page of the library's pagination.</summary>
public sealed class WebViewExampleTests(ExampleHosts hosts)
{
    private const string Runtime = ".runtime";

    /// <summary>The example hosts, by name.</summary>
    public static TheoryData<string> Examples { get; } = ["Wpf", "WinForms", "Maui"];

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task Should_load_the_library_s_stylesheets_from_its_published_assets(string example)
    {
        // Arrange
        await using var application = await hosts.StartApplicationAsync(example);
        var page = await application.GetOtherWebViewAsync([], Runtime);

        // Act
        var empty = await page.EvaluateAsync<string[]>("() => [...document.styleSheets].filter(sheet => sheet.cssRules.length === 0).map(sheet => sheet.href)");
        var primary = await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--primary-color').trim()");

        // Assert
        Assert.Multiple(
            () => Assert.True(empty.Length == 0, $"The page linked stylesheets that loaded no rules:{Environment.NewLine}{string.Join(Environment.NewLine, empty)}"),
            () => Assert.NotEmpty(primary));
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task Should_serve_every_script_the_library_publishes_as_JavaScript(string example)
    {
        // Arrange — a browser runs a module only when it is served as JavaScript, which a host's file server decides by
        // the file's extension
        await using var application = await hosts.StartApplicationAsync(example);
        var page = await application.GetOtherWebViewAsync([], Runtime);
        var root = Path.Combine(hosts.ExamplesFolder, example, "wwwroot");
        var scripts = Directory.EnumerateFiles(Path.Combine(root, "_content"), "*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".js", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .ToArray();

        // Act
        var refused = await page.EvaluateAsync<string[]>("""
            async scripts => (await Promise.all(scripts.map(async script => {
                const type = (await fetch(script)).headers.get('content-type') ?? '';
                return /^(text|application)\/javascript\b/.test(type) ? null : `${script}: ${type}`;
            }))).filter(script => script !== null)
            """, scripts);

        // Assert
        Assert.Multiple(
            () => Assert.NotEmpty(scripts),
            () => Assert.True(refused.Length == 0, $"The host served scripts as something other than JavaScript:{Environment.NewLine}{string.Join(Environment.NewLine, refused)}"));
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task Should_turn_a_page_of_the_library_s_pagination(string example)
    {
        // Arrange
        await using var application = await hosts.StartApplicationAsync(example);
        var page = await application.GetOtherWebViewAsync([], Runtime);
        var next = page.Locator(".forma-pagination-navigation-button-next:visible");

        // Act
        await next.ClickAsync();

        // Assert — the second of two pages is the last, so the step past it closes
        await Assertions.Expect(next).ToBeDisabledAsync();
    }
}
