namespace Forma.Blazor.Browser.Tests;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

/// <summary>The WebView check host, started as an application of its own with WebView2's DevTools port open, and the browser connection the check drives its web views through.</summary>
/// <remarks>The gate publishes the check host on Windows, the one platform WebView2 runs on, and names its folder in <c>FORMA_BROWSER_WEBVIEW</c>; each check starts an application of its own, so no check sees another's culture, and every web view shares one browser process and one data folder of the check's.</remarks>
public sealed class WebViewApplication : IAsyncDisposable
{
    private readonly Process _process;
    private readonly IPlaywright _playwright;
    private readonly string _dataFolder;

    private WebViewApplication(Process process, IPlaywright playwright, IBrowser browser, string dataFolder, string folder)
    {
        _process = process;
        _playwright = playwright;
        _dataFolder = dataFolder;
        Browser = browser;
        Folder = folder;
    }

    /// <summary>The browser every web view is a page of.</summary>
    public IBrowser Browser { get; }

    /// <summary>The folder the check host was published to.</summary>
    public string Folder { get; }

    /// <summary>Starts the check host, skipping the check where WebView2 can't run.</summary>
    /// <param name="environment">The environment variables the check host starts with beside its own, by name.</param>
    /// <returns>The application, its first web view <c>A</c> opening.</returns>
    public static async Task<WebViewApplication> StartAsync(IReadOnlyDictionary<string, string>? environment = null)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "WebView2 runs on Windows alone, so the web view's checks run in the Windows gate.");

        var folder = Path.GetFullPath(Environment.GetEnvironmentVariable("FORMA_BROWSER_WEBVIEW") ?? Path.Combine(BrowserHost.FindRepositoryRoot(), "artifacts", "browser", "webview"));

        if (!Directory.Exists(folder))
        {
            throw new InvalidOperationException($"{folder} holds no publish; the gate's browser step publishes it first.");
        }

        var port = BrowserHost.GetFreePort();
        var dataFolder = Directory.CreateTempSubdirectory("forma-webview-").FullName;
        var start = new ProcessStartInfo(Path.Combine(folder, "Forma.Blazor.Browser.WebView.exe"))
        {
            WorkingDirectory = folder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.Environment["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={port}";
        start.Environment["WEBVIEW2_USER_DATA_FOLDER"] = dataFolder;

        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
        {
            start.Environment[name] = value;
        }

        // What the host writes, its WebView2 runtime and each web view's initialization among it, read as it writes.
        var output = new ConcurrentQueue<string>();
        var process = new Process { StartInfo = start };
        process.OutputDataReceived += (_, args) => Record(output, args.Data);
        process.ErrorDataReceived += (_, args) => Record(output, args.Data);

        if (!process.Start())
        {
            process.Dispose();

            throw new InvalidOperationException("The WebView check host did not start.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var playwright = await Playwright.CreateAsync().ConfigureAwait(false);

        try
        {
            var endpoint = new Uri($"http://127.0.0.1:{port}/");
            await WaitForDevToolsAsync(process, endpoint, output).ConfigureAwait(false);
            var browser = await playwright.Chromium.ConnectOverCDPAsync(endpoint.ToString()).ConfigureAwait(false);

            return new WebViewApplication(process, playwright, browser, dataFolder, folder);
        }
        catch
        {
            playwright.Dispose();
            process.Kill(entireProcessTree: true);
            process.Dispose();

            throw;
        }
    }

    /// <summary>Finds the page of the web view <paramref name="name"/> names, once it renders interactively.</summary>
    /// <param name="name">The web view's name, <c>A</c> for the first.</param>
    /// <returns>The web view's page.</returns>
    public async Task<IPage> GetWebViewAsync(string name)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            // A snapshot, since a web view opening meanwhile adds its page to these lists.
            foreach (var page in Browser.Contexts.ToArray().SelectMany(context => context.Pages.ToArray()))
            {
                try
                {
                    if (await page.EvaluateAsync<string?>("() => document.querySelector('#web-view')?.textContent ?? null").ConfigureAwait(false) == name)
                    {
                        await Assertions.Expect(page.Locator("#renderer")).ToHaveTextAsync("WebView True").ConfigureAwait(false);

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

        throw new TimeoutException($"No web view named {name} rendered within 30 seconds.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Browser.CloseAsync().ConfigureAwait(false);
        _playwright.Dispose();

        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync().ConfigureAwait(false);
        }

        _process.Dispose();

        try
        {
            Directory.Delete(_dataFolder, recursive: true);
        }
        catch (IOException)
        {
            // WebView2 may still hold a file for a moment; the folder is under the system's temp directory.
        }
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

    private static async Task WaitForDevToolsAsync(Process process, Uri endpoint, ConcurrentQueue<string> output)
    {
        using var client = new HttpClient();
        var deadline = DateTime.UtcNow.AddMinutes(1);

        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The WebView check host exited with code {process.ExitCode} before its web view opened.{Describe(output)}");
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

        throw new TimeoutException($"The WebView check host's DevTools at {endpoint} did not answer within a minute.{Describe(output)}");
    }
}
