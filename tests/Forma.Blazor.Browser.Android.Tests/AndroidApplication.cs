namespace Forma.Blazor.Browser.Android.Tests;

using System.Diagnostics;

using Forma.Tests.Android;

/// <summary>The MAUI example host built for Android, installed on the device adb reaches and started afresh, and the browser connection a check drives its web view through.</summary>
/// <remarks>
/// The build is named by <c>FORMA_BROWSER_ANDROID</c>, which the mobile step sets, and installed once for the run; a check is skipped where
/// none is named or no device is attached.
/// </remarks>
public sealed class AndroidApplication : IAsyncDisposable
{
    private const string PackageName = "com.formaui.examples.maui";

    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);

    // An emulator without a GPU, as a hosted runner's, renders and answers an order slower than a desktop browser.
    private static readonly TimeSpan ExpectTimeout = TimeSpan.FromSeconds(30);

    private readonly IPlaywright _playwright;
    private readonly int _port;

    private AndroidApplication(IPlaywright playwright, int port, IPage page)
    {
        _playwright = playwright;
        _port = port;
        Page = page;
    }

    /// <summary>The page of the application's web view, rendering interactively.</summary>
    public IPage Page { get; }

    /// <summary>Starts the application afresh, its data cleared, and connects to its web view once it renders interactively.</summary>
    /// <returns>The application.</returns>
    public static async Task<AndroidApplication> StartAsync()
    {
        var build = Environment.GetEnvironmentVariable("FORMA_BROWSER_ANDROID");
        Assert.SkipWhen(string.IsNullOrEmpty(build) || !File.Exists(build), "No Android build of the MAUI example host is named; the mobile step builds one and names it in FORMA_BROWSER_ANDROID.");
        Assert.SkipUnless(await AndroidDevice.IsAttachedAsync().ConfigureAwait(false), "No Android device or emulator is attached.");
        Assertions.SetDefaultExpectTimeout((float)ExpectTimeout.TotalMilliseconds);

        await AndroidDevice.InstallAsync(build).ConfigureAwait(false);
        var port = await AndroidDevice.StartAsync(PackageName).ConfigureAwait(false);
        var playwright = await Playwright.CreateAsync().ConfigureAwait(false);

        try
        {
            var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{port}/").ConfigureAwait(false);
            var page = await WaitForPageAsync(browser).ConfigureAwait(false);

            try
            {
                await Assertions.Expect(page.Locator(".runtime")).ToHaveTextAsync("WebView True", new() { Timeout = (float)StartTimeout.TotalMilliseconds }).ConfigureAwait(false);
            }
            catch (PlaywrightException exception)
            {
                var log = await AndroidDevice.GetLogAsync(PackageName).ConfigureAwait(false);

                throw new TimeoutException($"The application's web view never rendered interactively.{await DescribeAsync(page, log).ConfigureAwait(false)}", exception);
            }

            return new AndroidApplication(playwright, port, page);
        }
        catch
        {
            playwright.Dispose();
            await AndroidDevice.StopAsync(PackageName, port).ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _playwright.Dispose();
        await AndroidDevice.StopAsync(PackageName, _port).ConfigureAwait(false);
    }

    // What the web view shows, what holds the device's focus, and what the application and its web view's console last wrote.
    private static async Task<string> DescribeAsync(IPage page, string log)
    {
        var content = await page.ContentAsync().ConfigureAwait(false);
        var newLine = Environment.NewLine;

        return $"{newLine}The page at {page.Url} holds:{newLine}{content[..Math.Min(content.Length, 1500)]}{newLine}The focus is on {await AndroidDevice.GetFocusedWindowAsync().ConfigureAwait(false)}.{newLine}The application and its web view's console last wrote:{newLine}{log}";
    }

    private static async Task<IPage> WaitForPageAsync(IBrowser browser)
    {
        var started = Stopwatch.StartNew();

        while (started.Elapsed < StartTimeout)
        {
            if (browser.Contexts.SelectMany(context => context.Pages).FirstOrDefault(page => page.Url.StartsWith("https://0.0.0.", StringComparison.Ordinal)) is { } page)
            {
                return page;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
        }

        throw new TimeoutException($"The application's web view showed no page within {StartTimeout.TotalMinutes} minutes.");
    }
}
