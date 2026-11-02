namespace Forma.Blazor.Browser.Android.Tests;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

/// <summary>The MAUI example host built for Android, installed on the device adb reaches and started afresh, with its web view's DevTools socket forwarded, and the browser connection a check drives its web view through.</summary>
/// <remarks>
/// Android's WebView opens a socket named for the application's process, which adb forwards to a port of this machine, and Chrome's
/// DevTools protocol reaches the web view through it as it reaches WebView2's. The build is named by <c>FORMA_BROWSER_ANDROID</c>, which
/// the mobile step sets, and installed once for the run; a check is skipped where none is named or no device is attached.
/// </remarks>
public sealed class AndroidApplication : IAsyncDisposable
{
    private const string PackageName = "com.formaui.examples.maui";
    private const string ShareSheetPackage = "com.android.intentresolver";

    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);
    private static readonly SemaphoreSlim Installing = new(1, 1);
    private static bool _isInstalled;

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
        Assert.SkipUnless((await RunAdbAsync("get-state").ConfigureAwait(false)).Trim() == "device", "No Android device or emulator is attached.");

        await InstallAsync(build).ConfigureAwait(false);

        // A share sheet an earlier check opened would stand in front of this one's.
        await RunAdbAsync("shell", "am", "force-stop", ShareSheetPackage).ConfigureAwait(false);
        await RunAdbAsync("shell", "pm", "clear", PackageName).ConfigureAwait(false);
        await RunAdbAsync("shell", "monkey", "-p", PackageName, "-c", "android.intent.category.LAUNCHER", "1").ConfigureAwait(false);

        var socket = await WaitForSocketAsync().ConfigureAwait(false);
        var port = GetFreePort();
        await RunAdbAsync("forward", $"tcp:{port}", $"localabstract:{socket}").ConfigureAwait(false);
        var playwright = await Playwright.CreateAsync().ConfigureAwait(false);

        try
        {
            var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{port}/").ConfigureAwait(false);
            var page = await WaitForPageAsync(browser).ConfigureAwait(false);
            await Assertions.Expect(page.Locator(".runtime")).ToHaveTextAsync("WebView True", new() { Timeout = 60000 }).ConfigureAwait(false);

            return new AndroidApplication(playwright, port, page);
        }
        catch
        {
            playwright.Dispose();
            await RunAdbAsync("forward", "--remove", $"tcp:{port}").ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>The activity Android shows in front, which names the share sheet while it is open.</summary>
    /// <returns>The window that holds the focus, as the window manager names it.</returns>
    public static async Task<string> GetFocusedWindowAsync()
        => (await RunAdbAsync("shell", "dumpsys", "window", "displays").ConfigureAwait(false))
            .Split('\n')
            .FirstOrDefault(line => line.Contains("mCurrentFocus", StringComparison.Ordinal))?.Trim() ?? string.Empty;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _playwright.Dispose();
        await RunAdbAsync("forward", "--remove", $"tcp:{_port}").ConfigureAwait(false);
        await RunAdbAsync("shell", "am", "force-stop", PackageName).ConfigureAwait(false);
    }

    private static async Task InstallAsync(string build)
    {
        await Installing.WaitAsync().ConfigureAwait(false);

        try
        {
            if (!_isInstalled)
            {
                // An emulator under load reports its own apps not responding, in a dialog that would take the focus from the checks.
                await RunAdbAsync("shell", "settings", "put", "global", "hide_error_dialogs", "1").ConfigureAwait(false);
                var output = await RunAdbAsync("install", "-r", build).ConfigureAwait(false);

                if (!output.Contains("Success", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"adb didn't install {build}:{Environment.NewLine}{output}");
                }

                _isInstalled = true;
            }
        }
        finally
        {
            Installing.Release();
        }
    }

    // The web view opens its socket once the application's first web view initializes, which an emulator's first start takes a while for.
    private static async Task<string> WaitForSocketAsync()
    {
        var started = Stopwatch.StartNew();

        while (started.Elapsed < StartTimeout)
        {
            var process = (await RunAdbAsync("shell", "pidof", PackageName).ConfigureAwait(false)).Trim();

            if (process.Length > 0)
            {
                var socket = $"webview_devtools_remote_{process}";

                if ((await RunAdbAsync("shell", "cat", "/proc/net/unix").ConfigureAwait(false)).Contains($"@{socket}", StringComparison.Ordinal))
                {
                    return socket;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new TimeoutException($"The application's web view opened no DevTools socket within {StartTimeout.TotalMinutes} minutes.");
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

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    // adb from the Android SDK ANDROID_HOME names, or the one on the path.
    private static async Task<string> RunAdbAsync(params string[] arguments)
    {
        var home = Environment.GetEnvironmentVariable("ANDROID_HOME");
        var adb = home is { Length: > 0 } && File.Exists(Path.Combine(home, "platform-tools", OperatingSystem.IsWindows() ? "adb.exe" : "adb"))
            ? Path.Combine(home, "platform-tools", OperatingSystem.IsWindows() ? "adb.exe" : "adb")
            : "adb";
        var start = new ProcessStartInfo(adb, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(start) ?? throw new InvalidOperationException("adb didn't start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);

        return $"{await output.ConfigureAwait(false)}{await error.ConfigureAwait(false)}";
    }
}
