namespace Forma.Tests.Hosts;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.Playwright;

/// <summary>The Android device or emulator adb reaches: the build a run is asked to check, installing it, starting an application afresh, forwarding its web view's DevTools socket to a port of this machine and connecting to the web view there, and reading what the device shows and logs.</summary>
/// <remarks>
/// Android's WebView opens a socket named for the application's process once the application's first web view initializes, which adb
/// forwards to a port of this machine, where Chrome's DevTools protocol reaches the web view as it reaches WebView2's. adb is the one in
/// the Android SDK <c>ANDROID_HOME</c> names, or the one on the path, and the device the one it reaches by default or
/// <c>ANDROID_SERIAL</c> names.
/// </remarks>
public static partial class AndroidDevice
{
    private const string ShareSheetPackage = "com.android.intentresolver";
    private const int LaunchAttemptCount = 3;
    private const int ShareSheetDismissalCount = 10;

    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ShareSheetPoll = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim InstallLock = new(1, 1);
    private static readonly HashSet<string> InstalledBuilds = new(StringComparer.Ordinal);
    private static readonly Lazy<Task<bool>> IsAttached = new(async () => (await RunAdbCoreAsync("get-state").ConfigureAwait(false)).Trim() == "device");

    /// <summary>The build of an application the environment variable <paramref name="variable"/> names for the run to check.</summary>
    /// <param name="variable">The variable's name.</param>
    /// <returns>The build's signed package, or <see langword="null"/> where the variable names none, which leaves the checks on Android to be skipped.</returns>
    /// <exception cref="InvalidOperationException">The variable names a file that doesn't exist, which fails the run rather than skipping what it was asked to check.</exception>
    public static string? GetBuild(string variable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);

        var build = Environment.GetEnvironmentVariable(variable);

        if (string.IsNullOrEmpty(build))
        {
            return null;
        }

        return File.Exists(build)
            ? build
            : throw new InvalidOperationException($"{variable} names {build}, which doesn't exist; the run checks the build it names, so a build named and missing fails the run.");
    }

    /// <summary>Connects to the web view whose DevTools socket is forwarded to <paramref name="port"/>, once it shows the application's page.</summary>
    /// <param name="playwright">The driver the connection is made through.</param>
    /// <param name="port">The port of this machine the web view's socket is forwarded to.</param>
    /// <param name="timeout">How long the web view may take to show the page.</param>
    /// <returns>The connection, whose closing ends what a check added to the page, and the application's page.</returns>
    /// <exception cref="TimeoutException">The web view showed no page within <paramref name="timeout"/>.</exception>
    public static async Task<(IBrowser Browser, IPage Page)> ConnectAsync(IPlaywright playwright, int port, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(playwright);

        var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{port}/").ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            // A snapshot, since the page opening meanwhile adds itself to these lists.
            if (browser.Contexts.ToArray().SelectMany(context => context.Pages.ToArray()).FirstOrDefault(page => page.Url.StartsWith("https://0.0.0.", StringComparison.Ordinal)) is { } page)
            {
                return (browser, page);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
        }

        await browser.CloseAsync().ConfigureAwait(false);

        throw new TimeoutException($"The application's web view showed no page within {timeout.TotalMinutes} minutes.");
    }

    /// <summary>Whether adb reached a device or emulator as the run first asked.</summary>
    /// <returns><see langword="true"/> where one was attached.</returns>
    /// <remarks>The first answer stands for the run, so a device that drops off adb for a moment later fails the check that reaches for it.</remarks>
    public static Task<bool> IsAttachedAsync()
        => IsAttached.Value;

    /// <summary>Installs the build once for the run, quieting the error dialogs an emulator under load shows for its own apps.</summary>
    /// <param name="build">The application's signed package.</param>
    /// <returns>A task that completes once the build is installed.</returns>
    public static async Task InstallAsync(string build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(build);

        await InstallLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (InstalledBuilds.Contains(build))
            {
                return;
            }

            await RunAdbAsync("shell", "settings", "put", "global", "hide_error_dialogs", "1").ConfigureAwait(false);
            var output = await RunAdbAsync("install", "-r", build).ConfigureAwait(false);

            if (!output.Contains("Success", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"adb didn't install {build}:{Environment.NewLine}{output}");
            }

            InstalledBuilds.Add(build);
        }
        finally
        {
            InstallLock.Release();
        }
    }

    /// <summary>Whether <paramref name="window"/>, as <see cref="GetFocusedWindowAsync"/> names it, is the share sheet, as Android's own resolver of intents or the system's names it.</summary>
    /// <param name="window">The window that holds the focus.</param>
    public static bool IsShareSheet(string window)
    {
        ArgumentNullException.ThrowIfNull(window);

        return window.Contains("Chooser", StringComparison.Ordinal) || window.Contains("intentresolver", StringComparison.Ordinal);
    }

    /// <summary>Starts the application afresh, its data cleared and any share sheet an earlier check opened closed, and forwards its web view's DevTools socket.</summary>
    /// <param name="package">The application's package name.</param>
    /// <returns>The port of this machine the web view's socket is forwarded to.</returns>
    /// <exception cref="InvalidOperationException">adb didn't start the application.</exception>
    public static async Task<int> StartAsync(string package)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(package);

        await CloseShareSheetAsync().ConfigureAwait(false);
        await RunAdbAsync("shell", "pm", "clear", package).ConfigureAwait(false);
        await LaunchAsync(package).ConfigureAwait(false);

        var socket = await WaitForSocketAsync(package).ConfigureAwait(false);

        return await ForwardAsync(socket).ConfigureAwait(false);
    }

    /// <summary>Stops the application and the forwarding of its web view's socket.</summary>
    /// <param name="package">The application's package name.</param>
    /// <param name="port">The port the web view's socket is forwarded to.</param>
    /// <returns>A task that completes once both stopped.</returns>
    public static async Task StopAsync(string package, int port)
    {
        await RunAdbAsync("forward", "--remove", $"tcp:{port}").ConfigureAwait(false);
        await RunAdbAsync("shell", "am", "force-stop", package).ConfigureAwait(false);
    }

    /// <summary>The window Android shows in front, which names the share sheet while it is open.</summary>
    /// <returns>The window that holds the focus, as the window manager names it.</returns>
    public static async Task<string> GetFocusedWindowAsync()
        => (await RunAdbAsync("shell", "dumpsys", "window", "displays").ConfigureAwait(false))
            .Split('\n')
            .FirstOrDefault(line => line.Contains("mCurrentFocus", StringComparison.Ordinal))?.Trim() ?? string.Empty;

    /// <summary>What the application, its web view's console among it, last wrote to the device's log.</summary>
    /// <param name="package">The application's package name.</param>
    /// <returns>The log's last lines from the application's process.</returns>
    public static async Task<string> GetLogAsync(string package)
    {
        var process = (await RunAdbAsync("shell", "pidof", package).ConfigureAwait(false)).Trim();

        return process.Length > 0
            ? await RunAdbAsync("logcat", "-d", "-t", "60", $"--pid={process}").ConfigureAwait(false)
            : "nothing, the application isn't running";
    }

    /// <summary>Runs adb with the arguments given.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>What adb wrote, its output followed by its errors.</returns>
    /// <remarks>A command still running after <see cref="CommandTimeout"/> is stopped with every process it started, and throws.</remarks>
    /// <exception cref="AndroidDeviceUnreachableException">adb couldn't reach the device, which the exception says with the devices adb lists.</exception>
    public static async Task<string> RunAdbAsync(params string[] arguments)
    {
        var output = await RunAdbCoreAsync(arguments).ConfigureAwait(false);

        if (UnreachableDevice().Match(output) is { Success: true } unreachableMatch)
        {
            var devices = await RunAdbCoreAsync("devices", "-l").ConfigureAwait(false);

            throw new AndroidDeviceUnreachableException($"adb {string.Join(' ', arguments)} couldn't reach the device: {unreachableMatch.Groups["reason"].Value}. adb lists:{Environment.NewLine}{devices.Trim()}");
        }

        return output;
    }

    // Runs adb and returns what it wrote, whatever it says about the device.
    private static async Task<string> RunAdbCoreAsync(params string[] arguments)
    {
        var tools = Environment.GetEnvironmentVariable("ANDROID_HOME") is { Length: > 0 } home ? Path.Combine(home, "platform-tools") : null;
        var name = OperatingSystem.IsWindows() ? "adb.exe" : "adb";
        var adb = tools is not null && File.Exists(Path.Combine(tools, name)) ? Path.Combine(tools, name) : "adb";
        var start = new ProcessStartInfo(adb, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = StartAdb(start);
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(CommandTimeout);

        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().ConfigureAwait(false);

            throw new TimeoutException($"adb {string.Join(' ', arguments)} didn't finish within {CommandTimeout.TotalMinutes} minutes; adb wrote:{Environment.NewLine}{await output.ConfigureAwait(false)}{await error.ConfigureAwait(false)}");
        }

        return $"{await output.ConfigureAwait(false)}{await error.ConfigureAwait(false)}";
    }

    // A share sheet still showing is dismissed with Back, as a reader dismisses it, before its package is stopped, since an
    // emulator whose share sheet is stopped while it shows can crash.
    private static async Task CloseShareSheetAsync()
    {
        for (var attempt = 0; attempt < ShareSheetDismissalCount && IsShareSheet(await GetFocusedWindowAsync().ConfigureAwait(false)); attempt++)
        {
            await RunAdbAsync("shell", "input", "keyevent", "KEYCODE_BACK").ConfigureAwait(false);
            await Task.Delay(ShareSheetPoll).ConfigureAwait(false);
        }

        await RunAdbAsync("shell", "am", "force-stop", ShareSheetPackage).ConfigureAwait(false);
    }

    // adb chooses the port and prints it first, so no other process takes it between its choice and adb's listening on it.
    private static async Task<int> ForwardAsync(string socket)
    {
        var output = await RunAdbAsync("forward", "tcp:0", $"localabstract:{socket}").ConfigureAwait(false);
        var answer = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        return int.TryParse(answer, NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            ? port
            : throw new InvalidOperationException($"adb didn't forward the web view's DevTools socket {socket}; it wrote:{Environment.NewLine}{output}");
    }

    // Starts the application's launcher activity, asking again where adb dropped the request before the device ran it.
    private static async Task LaunchAsync(string package)
    {
        var outputs = new List<string>();

        for (var attempt = 0; attempt < LaunchAttemptCount; attempt++)
        {
            var output = await RunAdbAsync("shell", "monkey", "-p", package, "-c", "android.intent.category.LAUNCHER", "1").ConfigureAwait(false);

            if (output.Contains("Events injected: 1", StringComparison.Ordinal))
            {
                return;
            }

            outputs.Add(output);
        }

        throw new InvalidOperationException($"adb didn't start {package} in {LaunchAttemptCount} attempts; it wrote:{Environment.NewLine}{string.Join(Environment.NewLine, outputs)}");
    }

    // The web view opens its socket once the application's first web view initializes, which an emulator's first start takes a while for.
    private static async Task<string> WaitForSocketAsync(string package)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < StartTimeout)
        {
            var process = (await RunAdbAsync("shell", "pidof", package).ConfigureAwait(false)).Trim();

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

        throw new TimeoutException($"{package}'s web view opened no DevTools socket within {StartTimeout.TotalMinutes} minutes.");
    }

    // What adb says where the device it addresses can't be reached, as each of its versions words it.
    [GeneratedRegex(@"^(?:adb(?:\.exe)?|error): (?<reason>device offline|no devices/emulators found|device '[^']*' not found|device unauthorized|device still authorizing)", RegexOptions.Multiline)]
    private static partial Regex UnreachableDevice();

    // adb is the one in the Android SDK ANDROID_HOME names, or the one on the path; where neither has one, the run fails saying so.
    private static Process StartAdb(ProcessStartInfo start)
    {
        try
        {
            return Process.Start(start) ?? throw new InvalidOperationException("adb didn't start.");
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException($"adb isn't in the platform-tools folder of the Android SDK ANDROID_HOME names, nor on the PATH: {exception.Message}", exception);
        }
    }
}
