namespace Forma.Blazor.Browser.Tests;

using System.Diagnostics;

using Forma.Tests.Android;

/// <summary>The Android check host, installed on the device adb reaches and started once for the run, whose one web view each component check on Android reaches through a connection of its own.</summary>
/// <remarks>A check's connection carries what the check adds to its page, a script run before the page or a route its requests take, so closing it at the check's end takes them back, as closing a web view does under WebView2.</remarks>
public sealed class AndroidCheckApplication : IAsyncDisposable
{
    private const string PackageName = "com.formaui.checks.android";

    // The size a check lays its page out at.
    private const int PageWidth = 1280;
    private const int PageHeight = 720;

    private static readonly TimeSpan PageTimeout = TimeSpan.FromMinutes(3);

    private readonly IPlaywright _playwright;
    private readonly int _port;

    private AndroidCheckApplication(IPlaywright playwright, int port)
    {
        _playwright = playwright;
        _port = port;
    }

    /// <summary>Installs the check host's build and starts it afresh, on a device whose screen holds a check's page.</summary>
    /// <param name="build">The check host's signed package.</param>
    /// <returns>The application.</returns>
    /// <exception cref="InvalidOperationException">The device's screen is smaller than a check's page.</exception>
    public static async Task<AndroidCheckApplication> StartAsync(string build)
    {
        await AndroidDevice.InstallAsync(build).ConfigureAwait(false);
        var port = await AndroidDevice.StartAsync(PackageName).ConfigureAwait(false);
        var application = new AndroidCheckApplication(await Playwright.CreateAsync().ConfigureAwait(false), port);

        try
        {
            var (browser, page) = await application.ConnectAsync().ConfigureAwait(false);
            var screen = await page.EvaluateAsync<int[]>("() => [innerWidth, innerHeight]").ConfigureAwait(false);
            await browser.CloseAsync().ConfigureAwait(false);

            if (screen[0] < PageWidth || screen[1] < PageHeight)
            {
                throw new InvalidOperationException($"The device's screen holds a page {screen[0]} by {screen[1]}, smaller than the {PageWidth} by {PageHeight} a check lays its page out at, and the web view drops wheel input past the screen's edge; the components are checked on a device whose screen holds the page, such as an emulator of 1280 by 800 points.");
            }
        }
        catch
        {
            await application.DisposeAsync().ConfigureAwait(false);

            throw;
        }

        return application;
    }

    /// <summary>Connects to the web view afresh, for one check.</summary>
    /// <returns>The connection, whose closing ends what the check added to the page, and the web view's page.</returns>
    public async Task<(IBrowser Browser, IPage Page)> ConnectAsync()
    {
        var browser = await _playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{_port}/").ConfigureAwait(false);
        var started = Stopwatch.StartNew();

        while (started.Elapsed < PageTimeout)
        {
            if (browser.Contexts.SelectMany(context => context.Pages).FirstOrDefault(page => page.Url.StartsWith("https://0.0.0.", StringComparison.Ordinal)) is { } page)
            {
                return (browser, page);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
        }

        await browser.CloseAsync().ConfigureAwait(false);

        throw new TimeoutException($"The Android check host's web view showed no page within {PageTimeout.TotalMinutes} minutes.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _playwright.Dispose();
        await AndroidDevice.StopAsync(PackageName, _port).ConfigureAwait(false);
    }
}
