namespace Forma.Blazor.Browser.Tests;

using static Forma.Blazor.Browser.Tests.CulturePages;

/// <summary>The Blazor Hybrid example hosts, built from Forma's packages: each starting from its publish, switching every web view and every native window's title, releasing a closed native window's web view and keeping its culture from one run to the next.</summary>
/// <remarks>The WPF and MAUI examples register the platform's web view first and Forma second, the Windows Forms one Forma first, so the examples hold both orders.</remarks>
[Collection(WebViewChecks.Name)]
public sealed class WebViewExampleTests(BrowserHost host)
{
    private const string Runtime = ".runtime";
    private const string Title = "Forma Example";
    private const string Welcome = "ברוכים הבאים";
    private const string PaginationLabel = "ניווט עמודים";
    private const string HebrewTitle = "דוגמת Forma";

    /// <summary>The example hosts, by name.</summary>
    public static TheoryData<string> Examples { get; } = ["Wpf", "WindowsForms", "Maui"];

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task Should_switch_every_web_view_and_native_window_title_with_strings_from_the_files_its_application_embeds(string example)
    {
        // Arrange
        await using var application = await StartAsync(example);
        var first = await OpenInEnglishAsync(application);
        await first.Locator(".open").ClickAsync();
        var second = await application.GetOtherWebViewAsync([first], Runtime);
        var windows = await WaitForWindowsAsync(application, 2);

        // Act
        await first.Locator(".hebrew").ClickAsync();

        // Assert
        await ExpectHebrewAsync(first);
        await ExpectHebrewAsync(second);
        await WaitForTitlesAsync(windows, HebrewTitle);
        Assert.False(Directory.Exists(Path.Combine(application.Folder, ".locales")), "The publish carries no folder of locale files, so the host's strings come from its assembly.");
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task Should_load_the_library_s_stylesheets_from_its_published_assets(string example)
    {
        // Arrange
        await using var application = await StartAsync(example);
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
    public async Task Should_release_a_closed_native_window_s_web_view_and_keep_switching_the_others(string example)
    {
        // Arrange
        await using var application = await StartAsync(example);
        var first = await OpenInEnglishAsync(application);
        var opened = await WaitForWindowsAsync(application, 1);
        await first.Locator(".open").ClickAsync();
        var second = await application.GetOtherWebViewAsync([first], Runtime);
        var other = (await WaitForWindowsAsync(application, 2)).Except(opened).Single();
        var closed = new TaskCompletionSource();
        second.Close += (_, _) => closed.TrySetResult();

        // Act
        NativeWindows.Close(other);
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        await first.Locator(".hebrew").ClickAsync();

        // Assert
        await ExpectHebrewAsync(first);
        await WaitForTitlesAsync(opened, HebrewTitle);
        Assert.False(application.HasExited, $"The application exited as its other native window closed. It wrote:{Environment.NewLine}{application.GetOutput(40)}");
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task Should_start_in_the_culture_its_last_run_saved(string example)
    {
        // Arrange
        await using (var previous = await StartAsync(example))
        {
            var page = await OpenInEnglishAsync(previous);
            await page.Locator(".hebrew").ClickAsync();
            await ExpectHebrewAsync(page);
            NativeWindows.Close((await WaitForWindowsAsync(previous, 1)).Single());
            await previous.WaitForExitAsync();
        }

        // Act
        await using var application = await StartAsync(example);
        var restarted = await application.GetOtherWebViewAsync([], Runtime);

        // Assert
        await ExpectHebrewAsync(restarted);
        await WaitForTitlesAsync(await WaitForWindowsAsync(application, 1), HebrewTitle);
        await restarted.Locator(".english").ClickAsync();
        await Assertions.Expect(restarted.Locator(".welcome")).ToHaveTextAsync("Welcome");
    }

    private Task<WebViewApplication> StartAsync(string example)
        => WebViewApplication.StartAsync(Path.Combine(host.ExamplesFolder, example), $"Forma.Examples.{example}");

    // The application's first web view, switched to English, which a run before may have left in another culture.
    private static async Task<IPage> OpenInEnglishAsync(WebViewApplication application)
    {
        var page = await application.GetOtherWebViewAsync([], Runtime);
        await page.Locator(".english").ClickAsync();
        await Assertions.Expect(page.Locator(".welcome")).ToHaveTextAsync("Welcome");
        await WaitForDocumentAsync(page, "en-US/ltr");
        await WaitForTitlesAsync(await WaitForWindowsAsync(application, 1), Title);

        return page;
    }

    // The host's own string, from the files its application embeds, the library's from the Hebrew pack, and the document's language and direction.
    private static async Task ExpectHebrewAsync(IPage page)
    {
        await Assertions.Expect(page.Locator(".welcome")).ToHaveTextAsync(Welcome);
        await Assertions.Expect(page.Locator("nav")).ToHaveAttributeAsync("aria-label", PaginationLabel);
        await WaitForDocumentAsync(page, "he/rtl");
    }

    // The application's native windows, once it shows as many as it is asked for.
    private static async Task<IReadOnlyList<nint>> WaitForWindowsAsync(WebViewApplication application, int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (NativeWindows.Find(application.ProcessId) is { } windows && windows.Count == count)
            {
                return windows;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        }

        throw new TimeoutException($"The application didn't show {count} native windows within 30 seconds; it shows {NativeWindows.Find(application.ProcessId).Count}.");
    }

    // A title follows a switch as the native window's thread gets to it; MAUI's native window keeps its caption in the system's ANSI
    // code page, while the title bar it draws shows the title whole.
    private static async Task WaitForTitlesAsync(IReadOnlyList<nint> windows, string title)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (windows.Any(window => NativeWindows.GetTitle(window) != NativeWindows.AsKept(window, title)))
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"The native windows' titles read {string.Join(", ", windows.Select(NativeWindows.GetTitle))} rather than {title} after 15 seconds.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        }
    }
}
