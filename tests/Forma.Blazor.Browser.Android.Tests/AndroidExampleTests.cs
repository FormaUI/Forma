namespace Forma.Blazor.Browser.Android.Tests;

/// <summary>The MAUI example host on Android, built from Forma's packages as the library's README sets an application up: its strings switching with the culture, its stylesheets loading from the application's package, and its sharing and printing reaching the platform's share sheet.</summary>
public sealed class AndroidExampleTests
{
    private const string Welcome = "ברוכים הבאים";
    private const string PaginationLabel = "ניווט עמודים";

    // The window the share sheet shows, as Android's own resolver of intents, or the system's, names it.
    private static bool IsShareSheet(string window)
        => window.Contains("Chooser", StringComparison.Ordinal) || window.Contains("intentresolver", StringComparison.Ordinal);

    private static async Task WaitForShareSheetAsync()
    {
        var window = string.Empty;

        for (var attempt = 0; attempt < 60 && !IsShareSheet(window); attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            window = await AndroidApplication.GetFocusedWindowAsync();
        }

        Assert.True(IsShareSheet(window), $"The share sheet never came to the front; the focus is on {window}.");
    }

    [Fact]
    public async Task Should_switch_the_web_view_with_strings_from_the_files_its_application_embeds_and_the_library_s_pack()
    {
        // Arrange
        await using var application = await AndroidApplication.StartAsync();
        var page = application.Page;
        await Assertions.Expect(page.Locator(".welcome")).ToHaveTextAsync("Welcome");

        // Act
        await page.Locator(".hebrew").ClickAsync();

        // Assert
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "he");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Assertions.Expect(page.Locator(".welcome")).ToHaveTextAsync(Welcome);
        await Assertions.Expect(page.Locator("nav")).ToHaveAttributeAsync("aria-label", PaginationLabel);
    }

    [Fact]
    public async Task Should_load_the_library_s_stylesheets_from_its_packaged_assets()
    {
        // Arrange
        await using var application = await AndroidApplication.StartAsync();

        // Act
        var empty = await application.Page.EvaluateAsync<string[]>("() => [...document.styleSheets].filter(sheet => sheet.cssRules.length === 0).map(sheet => sheet.href)");
        var primary = await application.Page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--primary-color').trim()");

        // Assert
        Assert.Multiple(
            () => Assert.True(empty.Length == 0, $"The page linked stylesheets that loaded no rules:{Environment.NewLine}{string.Join(Environment.NewLine, empty)}"),
            () => Assert.NotEmpty(primary));
    }

    [Theory]
    [InlineData("Share")]
    [InlineData("Print")]
    public async Task Should_hand_the_share_menu_s_command_to_the_platform_s_share_sheet(string command)
    {
        // Arrange
        await using var application = await AndroidApplication.StartAsync();
        var page = application.Page;
        await page.Locator(".forma-dropdown-menu-anchor button").ClickAsync();
        var item = page.GetByRole(AriaRole.Menuitem, new() { Name = command, Exact = true });
        Assert.False(IsShareSheet(await AndroidApplication.GetFocusedWindowAsync()), "A share sheet stood in front before the command ran.");

        // Act
        await item.ClickAsync();

        // Assert
        await WaitForShareSheetAsync();
    }
}
