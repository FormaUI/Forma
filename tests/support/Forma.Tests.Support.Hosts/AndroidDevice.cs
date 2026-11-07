namespace Forma.Tests.Hosts;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

/// <summary>The Android device or emulator adb reaches: installing a build, starting an application afresh, forwarding its web view's DevTools socket to a port of this machine, and reading what the device shows and logs.</summary>
/// <remarks>
/// Android's WebView opens a socket named for the application's process once the application's first web view initializes, which adb
/// forwards to a port of this machine, where Chrome's DevTools protocol reaches the web view as it reaches WebView2's. adb is the one in
/// the Android SDK <c>ANDROID_HOME</c> names, or the one on the path, and the device the one it reaches by default or
/// <c>ANDROID_SERIAL</c> names.
/// </remarks>
public static partial class AndroidDevice
{
    private const string ShareSheetPackage = "com.android.intentresolver";
    private const int LaunchAttempts = 3;

    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim Installing = new(1, 1);
    private static readonly HashSet<string> Installed = new(StringComparer.Ordinal);
    private static readonly Lazy<Task<bool>> Attached = new(async () => (await RunAdbCoreAsync("get-state").ConfigureAwait(false)).Trim() == "device");

    /// <summary>Whether adb reached a device or emulator as the run first asked.</summary>
    /// <returns><see langword="true"/> where one was attached.</returns>
    /// <remarks>The first answer stands for the run, so a device that drops off adb for a moment later fails the check that reaches for it.</remarks>
    public static Task<bool> IsAttachedAsync()
        => Attached.Value;

    /// <summary>Installs the build once for the run, quieting the error dialogs an emulator under load shows for its own apps.</summary>
    /// <param name="build">The application's signed package.</param>
    /// <returns>A task that completes once the build is installed.</returns>
    public static async Task InstallAsync(string build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(build);

        await Installing.WaitAsync().ConfigureAwait(false);

        try
        {
            if (Installed.Contains(build))
            {
                return;
            }

            await RunAdbAsync("shell", "settings", "put", "global", "hide_error_dialogs", "1").ConfigureAwait(false);
            var output = await RunAdbAsync("install", "-r", build).ConfigureAwait(false);

            if (!output.Contains("Success", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"adb didn't install {build}:{Environment.NewLine}{output}");
            }

            Installed.Add(build);
        }
        finally
        {
            Installing.Release();
        }
    }

    /// <summary>Starts the application afresh, its data cleared and any share sheet an earlier check opened closed, and forwards its web view's DevTools socket.</summary>
    /// <param name="package">The application's package name.</param>
    /// <returns>The port of this machine the web view's socket is forwarded to.</returns>
    /// <exception cref="InvalidOperationException">adb didn't start the application.</exception>
    public static async Task<int> StartAsync(string package)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(package);

        await RunAdbAsync("shell", "am", "force-stop", ShareSheetPackage).ConfigureAwait(false);
        await RunAdbAsync("shell", "pm", "clear", package).ConfigureAwait(false);
        await LaunchAsync(package).ConfigureAwait(false);

        var socket = await WaitForSocketAsync(package).ConfigureAwait(false);
        var port = GetFreePort();
        await RunAdbAsync("forward", $"tcp:{port}", $"localabstract:{socket}").ConfigureAwait(false);

        return port;
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
        var written = await RunAdbCoreAsync(arguments).ConfigureAwait(false);

        if (UnreachableDevice().Match(written) is { Success: true } unreachable)
        {
            var devices = await RunAdbCoreAsync("devices", "-l").ConfigureAwait(false);

            throw new AndroidDeviceUnreachableException($"adb {string.Join(' ', arguments)} couldn't reach the device: {unreachable.Groups["reason"].Value}. adb lists:{Environment.NewLine}{devices.Trim()}");
        }

        return written;
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

        using var process = Process.Start(start) ?? throw new InvalidOperationException("adb didn't start.");
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

    // Starts the application's launcher activity, asking again where adb dropped the request before the device ran it.
    private static async Task LaunchAsync(string package)
    {
        var written = new List<string>();

        for (var attempt = 0; attempt < LaunchAttempts; attempt++)
        {
            var output = await RunAdbAsync("shell", "monkey", "-p", package, "-c", "android.intent.category.LAUNCHER", "1").ConfigureAwait(false);

            if (output.Contains("Events injected: 1", StringComparison.Ordinal))
            {
                return;
            }

            written.Add(output);
        }

        throw new InvalidOperationException($"adb didn't start {package} in {LaunchAttempts} attempts; it wrote:{Environment.NewLine}{string.Join(Environment.NewLine, written)}");
    }

    // The web view opens its socket once the application's first web view initializes, which an emulator's first start takes a while for.
    private static async Task<string> WaitForSocketAsync(string package)
    {
        var started = Stopwatch.StartNew();

        while (started.Elapsed < StartTimeout)
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

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
