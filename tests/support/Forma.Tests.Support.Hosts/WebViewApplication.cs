namespace Forma.Tests.Hosts;

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

using Microsoft.Playwright;

using Xunit;

/// <summary>A Blazor Hybrid application started from its publish with WebView2's DevTools port open, and the browser connection a check drives its web views through.</summary>
/// <remarks>An application is published on Windows, the one platform WebView2 runs on; each check starts an application of its own, so no check sees another's culture, and every web view shares one browser process and one data folder of the check's. An application stops once its browser has let go of its DevTools port, which a policy can fix for every start of the application, so the next start finds the port and the data folder free; a start that fails stops what it started the same way.</remarks>
public sealed class WebViewApplication : IAsyncDisposable
{
    private readonly HostProcess _process;
    private readonly IPlaywright _playwright;
    private readonly string _dataFolder;
    private readonly int _port;

    private WebViewApplication(HostProcess process, IPlaywright playwright, IBrowser browser, string dataFolder, int port, string folder)
    {
        _process = process;
        _playwright = playwright;
        _dataFolder = dataFolder;
        _port = port;
        Browser = browser;
        Folder = folder;
    }

    /// <summary>The application's process.</summary>
    public int ProcessId
        => _process.Id;

    /// <summary>Whether the application's process has exited.</summary>
    public bool HasExited
        => _process.HasExited;

    /// <summary>The browser every web view is a page of.</summary>
    public IBrowser Browser { get; }

    /// <summary>The folder the application was published to.</summary>
    public string Folder { get; }

    /// <summary>The last of what the application wrote, its log among it, a line apiece.</summary>
    /// <param name="count">How many lines to give at most.</param>
    /// <returns>The lines, the newest last.</returns>
    public string GetOutput(int count)
        => _process.GetOutput(count);

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

        var start = new ProcessStartInfo(Path.Combine(folder, $"{name}.exe")) { WorkingDirectory = folder };

        // Where the machine's WebView2 policy names the application's DevTools port, the variables stay unset, since WebView2 reads the
        // policy only where none is set.
        var policyPort = GetPolicyPort(name);
        var port = policyPort ?? LocalPorts.GetFree();
        var dataFolder = Directory.CreateTempSubdirectory("forma-webview-").FullName;

        if (policyPort is null)
        {
            start.Environment["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={port}";
            start.Environment["WEBVIEW2_USER_DATA_FOLDER"] = dataFolder;
        }

        foreach (var (variable, value) in environment ?? new Dictionary<string, string>())
        {
            start.Environment[variable] = value;
        }

        HostProcess? process = null;
        IPlaywright? playwright = null;

        try
        {
            // What the host writes, its WebView2 runtime and each web view's initialization among it, read as it writes.
            process = HostProcess.Start(start, name);
            var endpoint = new Uri($"http://127.0.0.1:{port}/");
            await WaitForDevToolsAsync(process, endpoint, dataFolder).ConfigureAwait(false);
            playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            var browser = await playwright.Chromium.ConnectOverCDPAsync(endpoint.ToString()).ConfigureAwait(false);

            return new WebViewApplication(process, playwright, browser, dataFolder, port, folder);
        }
        catch (Exception startFailure)
        {
            try
            {
                await StopAsync(playwright, process, port, dataFolder).ConfigureAwait(false);
            }
            catch (Exception stopFailure)
            {
                throw new AggregateException(startFailure, stopFailure);
            }

            throw;
        }
    }

    /// <summary>Finds the page of the web view <paramref name="predicate"/> picks, once one is there, asking each page again as web views open and load.</summary>
    /// <param name="predicate">Whether a page is the web view's, asked of a page that may still be loading or may close meanwhile.</param>
    /// <param name="description">What the web view does, which a failure to find it names it by, as <c>named A rendered interactively</c>.</param>
    /// <param name="timeout">How long to look.</param>
    /// <returns>The web view's page.</returns>
    /// <exception cref="TimeoutException">No page was the web view's within <paramref name="timeout"/>.</exception>
    public async Task<IPage> FindWebViewAsync(Func<IPage, Task<bool>> predicate, string description, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            // A snapshot, since a web view opening meanwhile adds its page to these lists.
            foreach (var page in Browser.Contexts.ToArray().SelectMany(context => context.Pages.ToArray()))
            {
                try
                {
                    if (await predicate(page).ConfigureAwait(false))
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

        throw new TimeoutException($"No web view {description} within {timeout.TotalSeconds} seconds.{_process.DescribeOutput()}");
    }

    /// <summary>Finds the page of a web view other than <paramref name="knownPages"/>, once <paramref name="runtime"/> on it reads that it renders interactively.</summary>
    /// <param name="knownPages">The web views' pages already found.</param>
    /// <param name="runtime">The selector of the element the page shows its runtime in.</param>
    /// <returns>The web view's page.</returns>
    public Task<IPage> GetOtherWebViewAsync(IReadOnlyCollection<IPage> knownPages, string runtime)
    {
        ArgumentNullException.ThrowIfNull(knownPages);

        return FindWebViewAsync(
            async page => !knownPages.Contains(page) && await page.EvaluateAsync<string?>("selector => document.querySelector(selector)?.textContent ?? null", runtime).ConfigureAwait(false) == "WebView True",
            "other than those found rendered interactively",
            TimeSpan.FromMinutes(1));
    }

    /// <summary>Waits a minute at most for the application's process to exit.</summary>
    /// <returns>A task that completes once it exited.</returns>
    public Task WaitForExitAsync()
        => _process.WaitForExitAsync(TimeSpan.FromMinutes(1));

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Browser.IsConnected)
            {
                await Browser.CloseAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await StopAsync(_playwright, _process, _port, _dataFolder).ConfigureAwait(false);
        }
    }

    // Takes back what a start made, each part however the one before it ends: the driver, the application with every process it
    // started, the DevTools port its browser holds until it has ended, and the data folder.
    private static async Task StopAsync(IPlaywright? playwright, HostProcess? process, int port, string dataFolder)
    {
        try
        {
            playwright?.Dispose();
        }
        finally
        {
            try
            {
                if (process is not null)
                {
                    await process.DisposeAsync().ConfigureAwait(false);
                    await WaitForPortReleaseAsync(port).ConfigureAwait(false);
                }
            }
            finally
            {
                DeleteDataFolder(dataFolder);
            }
        }
    }

    private static void DeleteDataFolder(string dataFolder)
    {
        try
        {
            Directory.Delete(dataFolder, recursive: true);
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

    // The command line of the browser process WebView2 started for the data folder, which names the arguments it took, or of every
    // WebView2 browser process where none is the data folder's.
    private static async Task<string> DescribeBrowserAsync(string dataFolder)
    {
        var query = new ProcessStartInfo("pwsh", ["-NoProfile", "-Command", $"$browsers = Get-CimInstance Win32_Process -Filter \"Name='msedgewebview2.exe'\" | Where-Object {{ $_.CommandLine -notlike '*--type=*' }}; $own = $browsers | Where-Object {{ $_.CommandLine -like '*{dataFolder}*' }}; if ($own) {{ $own.CommandLine }} elseif ($browsers) {{ 'None for its data folder; every WebView2 browser process:'; $browsers.CommandLine }}"])
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        using var describeProcess = Process.Start(query);

        if (describeProcess is null)
        {
            return string.Empty;
        }

        var commandLine = await describeProcess.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        await describeProcess.WaitForExitAsync().ConfigureAwait(false);

        return commandLine.Length == 0 ? " WebView2 runs no browser process at all." : $" WebView2's browser process started as:{Environment.NewLine}{commandLine.Trim()}";
    }

    // A DevTools port that never answers fails with what the host wrote and the browser process WebView2 started, which names the
    // arguments it took.
    private static async Task WaitForDevToolsAsync(HostProcess process, Uri endpoint, string dataFolder)
    {
        try
        {
            await process.WaitForAnswerAsync(new Uri(endpoint, "json/version")).ConfigureAwait(false);
        }
        catch (TimeoutException exception)
        {
            throw new TimeoutException($"{exception.Message}{await DescribeBrowserAsync(dataFolder).ConfigureAwait(false)}", exception);
        }
    }
}
