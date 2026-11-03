namespace Forma.Tests.Hosts;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

using Microsoft.Playwright;

using Xunit;

/// <summary>A Blazor Hybrid application started from its publish with WebView2's DevTools port open, and the browser connection a check drives its web views through.</summary>
/// <remarks>An application is published on Windows, the one platform WebView2 runs on; each check starts an application of its own, so no check sees another's culture, and every web view shares one browser process and one data folder of the check's. An application stops once its browser has let go of its DevTools port, which a policy can fix for every start of the application, so the next start finds the port and the data folder free.</remarks>
public sealed class WebViewApplication : IAsyncDisposable
{
    private readonly Process _process;
    private readonly IPlaywright _playwright;
    private readonly string _dataFolder;
    private readonly int _port;
    private readonly ConcurrentQueue<string> _output;

    private WebViewApplication(Process process, IPlaywright playwright, IBrowser browser, string dataFolder, int port, string folder, ConcurrentQueue<string> output)
    {
        _process = process;
        _playwright = playwright;
        _dataFolder = dataFolder;
        _port = port;
        _output = output;
        Browser = browser;
        Folder = folder;
    }

    /// <summary>The application's process.</summary>
    public int ProcessId
        => _process.Id;

    /// <summary>Whether the application's process has exited.</summary>
    public bool HasExited
        => _process.HasExited;

    /// <summary>The last of what the application wrote, its log among it, a line apiece.</summary>
    /// <param name="count">How many lines to give at most.</param>
    /// <returns>The lines, the newest last.</returns>
    public string GetOutput(int count)
        => string.Join(Environment.NewLine, _output.TakeLast(count));

    /// <summary>The browser every web view is a page of.</summary>
    public IBrowser Browser { get; }

    /// <summary>The folder the application was published to.</summary>
    public string Folder { get; }

    /// <summary>Starts the application <paramref name="name"/> names from its publish in <paramref name="folder"/>, skipping the check where WebView2 can't run.</summary>
    /// <param name="folder">The folder the application was published to.</param>
    /// <param name="name">The application's name, its executable's without the extension.</param>
    /// <param name="environment">The environment variables the application starts with beside its own, by name.</param>
    /// <returns>The application, its first web view opening.</returns>
    public static async Task<WebViewApplication> StartAsync(string folder, string name, IReadOnlyDictionary<string, string>? environment = null)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "WebView2 runs on Windows alone, so the web view's checks run in the Windows gate.");

        folder = Path.GetFullPath(folder);

        if (!Directory.Exists(folder))
        {
            throw new InvalidOperationException($"{folder} holds no publish; the gate's browser step publishes it first.");
        }

        var dataFolder = Directory.CreateTempSubdirectory("forma-webview-").FullName;
        var start = new ProcessStartInfo(Path.Combine(folder, $"{name}.exe"))
        {
            WorkingDirectory = folder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // Where the machine's WebView2 policy names the application's DevTools port, the variables stay unset, since WebView2 reads the
        // policy only where none is set.
        var policyPort = GetPolicyPort(name);
        var port = policyPort ?? LocalPorts.GetFree();

        if (policyPort is null)
        {
            start.Environment["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={port}";
            start.Environment["WEBVIEW2_USER_DATA_FOLDER"] = dataFolder;
        }

        foreach (var (variable, value) in environment ?? new Dictionary<string, string>())
        {
            start.Environment[variable] = value;
        }

        // What the host writes, its WebView2 runtime and each web view's initialization among it, read as it writes.
        var output = new ConcurrentQueue<string>();
        var process = new Process { StartInfo = start };
        process.OutputDataReceived += (_, args) => Record(output, args.Data);
        process.ErrorDataReceived += (_, args) => Record(output, args.Data);

        if (!process.Start())
        {
            process.Dispose();

            throw new InvalidOperationException($"{name} did not start.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var playwright = await Playwright.CreateAsync().ConfigureAwait(false);

        try
        {
            var endpoint = new Uri($"http://127.0.0.1:{port}/");
            await WaitForDevToolsAsync(name, process, endpoint, output, dataFolder).ConfigureAwait(false);
            var browser = await playwright.Chromium.ConnectOverCDPAsync(endpoint.ToString()).ConfigureAwait(false);

            return new WebViewApplication(process, playwright, browser, dataFolder, port, folder, output);
        }
        catch
        {
            playwright.Dispose();
            process.Kill(entireProcessTree: true);
            process.Dispose();

            throw;
        }
    }

    /// <summary>Finds the page of a web view other than <paramref name="known"/>, once <paramref name="runtime"/> on it reads that it renders interactively.</summary>
    /// <param name="known">The web views' pages already found.</param>
    /// <param name="runtime">The selector of the element the page shows its runtime in.</param>
    /// <returns>The web view's page.</returns>
    public async Task<IPage> GetOtherWebViewAsync(IReadOnlyCollection<IPage> known, string runtime)
    {
        ArgumentNullException.ThrowIfNull(known);

        var deadline = DateTime.UtcNow.AddMinutes(1);

        while (DateTime.UtcNow < deadline)
        {
            // A snapshot, since a web view opening meanwhile adds its page to these lists.
            foreach (var page in Browser.Contexts.ToArray().SelectMany(context => context.Pages.ToArray()).Where(page => !known.Contains(page)))
            {
                try
                {
                    if (await page.EvaluateAsync<string?>("selector => document.querySelector(selector)?.textContent ?? null", runtime).ConfigureAwait(false) == "WebView True")
                    {
                        return page;
                    }
                }
                catch (PlaywrightException)
                {
                    // A page still loading, or one that closed meanwhile; the next pass asks again.
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        }

        throw new TimeoutException($"No other web view rendered interactively within a minute.{Describe(_output)}");
    }

    /// <summary>Waits a minute at most for the application's process to exit.</summary>
    /// <returns>A task that completes once it exited.</returns>
    public Task WaitForExitAsync()
        => _process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(1));

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Browser.IsConnected)
        {
            await Browser.CloseAsync().ConfigureAwait(false);
        }

        _playwright.Dispose();

        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync().ConfigureAwait(false);
        }

        _process.Dispose();
        await WaitForPortReleaseAsync(_port).ConfigureAwait(false);

        try
        {
            Directory.Delete(_dataFolder, recursive: true);
        }
        catch (IOException)
        {
            // WebView2 may still hold a file for a moment; the folder is under the system's temp directory.
        }
    }

    // The DevTools port a WebView2 policy names for the application, from FORMA_WEBVIEW_POLICY_PORTS, as name=port pairs separated by ';'.
    private static int? GetPolicyPort(string name)
        => (Environment.GetEnvironmentVariable("FORMA_WEBVIEW_POLICY_PORTS") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(pair => pair.Length == 2 && pair[0] == name && int.TryParse(pair[1], NumberStyles.None, CultureInfo.InvariantCulture, out _))
            .Select(pair => (int?)int.Parse(pair[1], CultureInfo.InvariantCulture))
            .FirstOrDefault();

    // The browser process holds its DevTools port until it has ended, which a stopped application's browser can take a while to.
    private static async Task WaitForPortReleaseAsync(int port)
    {
        var deadline = DateTime.UtcNow.AddMinutes(1);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();

                return;
            }
            catch (SocketException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
            }
        }

        throw new TimeoutException($"The web view's browser still held its DevTools port {port} a minute after its application stopped.");
    }

    private static void Record(ConcurrentQueue<string> output, string? line)
    {
        if (line is not null)
        {
            output.Enqueue(line);
        }
    }

    private static string Describe(ConcurrentQueue<string> output)
        => output.IsEmpty ? " It wrote nothing." : $" It wrote:{Environment.NewLine}{string.Join(Environment.NewLine, output)}";

    // The command line of the browser process WebView2 started for the data folder, which names the arguments it took, or of every
    // WebView2 browser process where none is the data folder's.
    private static async Task<string> DescribeBrowserAsync(string dataFolder)
    {
        var query = new ProcessStartInfo("pwsh", ["-NoProfile", "-Command", $"$browsers = Get-CimInstance Win32_Process -Filter \"Name='msedgewebview2.exe'\" | Where-Object {{ $_.CommandLine -notlike '*--type=*' }}; $own = $browsers | Where-Object {{ $_.CommandLine -like '*{dataFolder}*' }}; if ($own) {{ $own.CommandLine }} elseif ($browsers) {{ 'None for its data folder; every WebView2 browser process:'; $browsers.CommandLine }}"])
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        using var describing = Process.Start(query);

        if (describing is null)
        {
            return string.Empty;
        }

        var commandLine = await describing.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        await describing.WaitForExitAsync().ConfigureAwait(false);

        return commandLine.Length == 0 ? " WebView2 runs no browser process at all." : $" WebView2's browser process started as:{Environment.NewLine}{commandLine.Trim()}";
    }

    private static async Task WaitForDevToolsAsync(string name, Process process, Uri endpoint, ConcurrentQueue<string> output, string dataFolder)
    {
        using var client = new HttpClient();
        var deadline = DateTime.UtcNow.AddMinutes(1);

        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"{name} exited with code {process.ExitCode} before its web view opened.{Describe(output)}");
            }

            try
            {
                using var response = await client.GetAsync(new Uri(endpoint, "json/version")).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // The web view's browser isn't listening yet; the next attempt asks again.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
        }

        throw new TimeoutException($"{name}'s DevTools at {endpoint} did not answer within a minute.{Describe(output)}{await DescribeBrowserAsync(dataFolder).ConfigureAwait(false)}");
    }
}
