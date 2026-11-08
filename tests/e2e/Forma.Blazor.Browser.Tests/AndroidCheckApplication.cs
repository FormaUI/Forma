namespace Forma.Blazor.Browser.Tests;

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
        IPlaywright? playwright = null;

        try
        {
            playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            var (browser, page) = await AndroidDevice.ConnectAsync(playwright, port, PageTimeout).ConfigureAwait(false);
            var screen = await page.EvaluateAsync<int[]>("() => [innerWidth, innerHeight]").ConfigureAwait(false);
            await browser.CloseAsync().ConfigureAwait(false);

            if (screen[0] < PageWidth || screen[1] < PageHeight)
            {
                throw new InvalidOperationException($"The device's screen holds a page {screen[0]} by {screen[1]}, smaller than the {PageWidth} by {PageHeight} a check lays its page out at, and the web view drops wheel input past the screen's edge; the components are checked on a device whose screen holds the page, such as an emulator of 1280 by 800 points.");
            }

            return new AndroidCheckApplication(playwright, port);
        }
        catch
        {
            await StopAsync(playwright, port).ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>Connects to the web view afresh, for one check.</summary>
    /// <returns>The connection, whose closing ends what the check added to the page, and the web view's page.</returns>
    public Task<(IBrowser Browser, IPage Page)> ConnectAsync()
        => AndroidDevice.ConnectAsync(_playwright, _port, PageTimeout);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await StopAsync(_playwright, _port).ConfigureAwait(false);

    // The driver goes, and the application and its socket's forwarding stop however the driver's going ends.
    private static async Task StopAsync(IPlaywright? playwright, int port)
    {
        try
        {
            playwright?.Dispose();
        }
        finally
        {
            await AndroidDevice.StopAsync(PackageName, port).ConfigureAwait(false);
        }
    }
}
