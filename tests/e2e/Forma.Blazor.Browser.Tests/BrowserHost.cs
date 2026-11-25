namespace Forma.Blazor.Browser.Tests;

using System.Net.NetworkInformation;

using Forma.Tests.Shared;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>The published host the checks drive, running on free ports in each of its modes, the published full client served as a standalone app, the published components host and example hosts, each started as the first check asks for its address, and the browser they drive them in.</summary>
/// <remarks>The host runs with islands and culture addresses, with interactive routing and culture addresses, with islands switching the culture state alone, with islands persisting a switch in the culture cookie, with islands and culture addresses beside an independent client culture, and with the recording router in each runtime. The gate publishes the host, the full client, the components host and the example hosts and names their folders in <c>FORMA_BROWSER_HOST</c>, <c>FORMA_BROWSER_FULL</c>, <c>FORMA_BROWSER_COMPONENTS</c> and <c>FORMA_BROWSER_EXAMPLES</c>; <c>FORMA_BROWSER_VISIBLE</c> opens a visible window instead of a headless one.</remarks>
public sealed class BrowserHost : IAsyncLifetime
{
    private readonly List<WebHostProcess> _hosts = [];
    private readonly Lock _hostsLock = new();
    private readonly Dictionary<string, Task<Uri>> _hostStarts = [];
    private readonly HttpClient _client = new();
    private readonly Lock _webViewLock = new();
    private readonly Lock _androidLock = new();
    private readonly Lock _webKitLock = new();
    private Task<WebViewApplication>? _webView;
    private IPage? _webViewControl;
    private Task<AndroidCheckApplication>? _android;
    private Task<IBrowser>? _webKit;
    private IPlaywright? _playwright;
    private WebApplication? _standalone;

    // Each publish is found as the first check that runs on it asks for it, so a run of some checks needs only the publishes they run on.
    private readonly Lazy<string> _hostFolder = new(() => GetFolder("FORMA_BROWSER_HOST", Path.Combine("artifacts", "browser", "host")));
    private readonly Lazy<string> _fullClientFolder = new(() => GetFolder("FORMA_BROWSER_FULL", Path.Combine("artifacts", "browser", "full")));
    private readonly Lazy<string> _componentsFolder = new(() => GetFolder("FORMA_BROWSER_COMPONENTS", Path.Combine("artifacts", "browser", "components")));
    private readonly Lazy<string> _examplesFolder = new(() => GetFolder("FORMA_BROWSER_EXAMPLES", Path.Combine("artifacts", "browser", "examples")));

    /// <summary>The repository the checks run in.</summary>
    public string RepositoryRoot { get; } = RepositoryFolder.Find();

    /// <summary>The folder the host was published to.</summary>
    public string HostFolder
        => _hostFolder.Value;

    /// <summary>The folder the full client was published to.</summary>
    public string FullClientFolder
        => _fullClientFolder.Value;

    /// <summary>The folder the components host was published to.</summary>
    public string ComponentsFolder
        => _componentsFolder.Value;

    /// <summary>The folder each example host was published to a folder of its own in.</summary>
    public string ExamplesFolder
        => _examplesFolder.Value;

    /// <summary>One component check's turn in a web view of <see cref="GetWebViewApplicationAsync"/>, which runs one check at a time, since every web view of a WPF application dispatches on its UI thread.</summary>
    public SemaphoreSlim WebViewTurn { get; } = new(1, 1);

    /// <summary>One component check's turn in the web view of <see cref="GetAndroidApplicationAsync"/>, which shows one page at a time.</summary>
    public SemaphoreSlim AndroidTurn { get; } = new(1, 1);

    /// <summary>The browser the checks open their pages in.</summary>
    public IBrowser Browser { get; private set; } = default!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        Browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Channel = "chrome",
            Headless = Environment.GetEnvironmentVariable("FORMA_BROWSER_VISIBLE") is null,
        }).ConfigureAwait(false);
    }

    /// <summary>The address of the host whose pages are islands under a static router, switching through culture addresses, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetAddressAsync()
        => GetStartedHostAsync(nameof(GetAddressAsync), () => StartAsync(routes: null, culture: null));

    /// <summary>The address of the host whose router is interactive on the Server, switching through culture addresses, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetInteractiveAddressAsync()
        => GetStartedHostAsync(nameof(GetInteractiveAddressAsync), () => StartAsync(routes: "interactive", culture: null));

    /// <summary>The address of the host whose pages are islands, switching the culture state alone, which renders every request in <c>en-US</c>, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetStateAddressAsync()
        => GetStartedHostAsync(nameof(GetStateAddressAsync), () => StartAsync(routes: null, culture: "state", requestCulture: "en-US"));

    /// <summary>The address of the host whose pages are islands, switching the culture state alone, which renders every request in the invariant culture, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetInvariantStateAddressAsync()
        => GetStartedHostAsync(nameof(GetInvariantStateAddressAsync), () => StartAsync(routes: null, culture: "state", requestCulture: "invariant"));

    /// <summary>The address of the host whose pages are islands, persisting a switch in the culture cookie request localization reads, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetCookieAddressAsync()
        => GetStartedHostAsync(nameof(GetCookieAddressAsync), () => StartAsync(routes: null, culture: "cookie"));

    /// <summary>The address of the host whose pages are islands switching through culture addresses, beside a client culture each WebAssembly runtime keeps as its own, with culture capture off, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetIndependentAddressAsync()
        => GetStartedHostAsync(nameof(GetIndependentAddressAsync), () => StartAsync(routes: null, culture: null, arrangement: "independent"));

    /// <summary>The address the full client is served at as a standalone app, with no server of its own, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetStandaloneAddressAsync()
        => GetStartedHostAsync(nameof(GetStandaloneAddressAsync), StartStandaloneAsync);

    /// <summary>The address of the components host, whose check pages each render one component in the runtime their address names, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetComponentsAddressAsync()
        => GetStartedHostAsync(nameof(GetComponentsAddressAsync), () => StartProcessAsync(ComponentsFolder, "Forma.Blazor.Browser.Components.dll", "_check/ready", []));

    /// <summary>The address of the example host whose client follows the page's culture, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetCoordinatedExampleAddressAsync()
        => GetStartedHostAsync(nameof(GetCoordinatedExampleAddressAsync), () => StartExampleAsync("Coordinated"));

    /// <summary>The address of the example host whose client's culture is its own, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetIndependentExampleAddressAsync()
        => GetStartedHostAsync(nameof(GetIndependentExampleAddressAsync), () => StartExampleAsync("Independent"));

    /// <summary>The address of the host rendering the recording router on the Server, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetNavigationServerAddressAsync()
        => GetStartedHostAsync(nameof(GetNavigationServerAddressAsync), () => StartAsync(routes: "navigation-server", culture: null));

    /// <summary>The address of the host rendering the recording router under WebAssembly, started as the first check asks for it.</summary>
    /// <returns>The address.</returns>
    public Task<Uri> GetNavigationWebAssemblyAddressAsync()
        => GetStartedHostAsync(nameof(GetNavigationWebAssemblyAddressAsync), () => StartAsync(routes: "navigation-webassembly", culture: null));

    /// <summary>Opens a page in a context of its own, which shares no cache, storage or cookie with another check's.</summary>
    /// <param name="address">The host the page's relative addresses resolve against; the islands host where none is given.</param>
    /// <returns>The page and the context that owns it.</returns>
    public async Task<(IBrowserContext Context, IPage Page)> OpenPageAsync(Uri? address = null)
    {
        var context = await Browser.NewContextAsync(new() { BaseURL = (address ?? await GetAddressAsync().ConfigureAwait(false)).ToString() }).ConfigureAwait(false);
        await PolicyViolations.WatchAsync(context).ConfigureAwait(false);
        var page = await context.NewPageAsync().ConfigureAwait(false);
        WatchConnectionFailures(page, "ERR_NO_BUFFER_SPACE");

        return (context, page);
    }

    /// <summary>Opens a page in WebKit, the engine of Safari and of Apple's web views, in a context of its own.</summary>
    /// <param name="address">The host the page's relative addresses resolve against.</param>
    /// <returns>The page and the context that owns it.</returns>
    public async Task<(IBrowserContext Context, IPage Page)> OpenWebKitPageAsync(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var context = await (await GetWebKitAsync().ConfigureAwait(false)).NewContextAsync(new() { BaseURL = address.ToString() }).ConfigureAwait(false);
        await PolicyViolations.WatchAsync(context).ConfigureAwait(false);
        var page = await context.NewPageAsync().ConfigureAwait(false);
        WatchConnectionFailures(page, "Could not connect to server");

        return (context, page);
    }

    /// <summary>Asks a host how many of its delayed switches have settled, past the browser, which a check may hold offline.</summary>
    /// <param name="address">The host asked.</param>
    /// <returns>The switches settled so far.</returns>
    public async Task<int> GetDelayedSwitchesAsync(Uri address)
        => int.Parse(await _client.GetStringAsync(new Uri(address, "_check/delayed")).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Asks a host how many of its settled delayed switches failed.</summary>
    /// <param name="address">The host asked.</param>
    /// <returns>The switches failed so far.</returns>
    public async Task<int> GetDelayedSwitchFailuresAsync(Uri address)
        => int.Parse(await _client.GetStringAsync(new Uri(address, "_check/delayed-failures")).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Releases the switches a host holds, past the browser, which a check may hold offline.</summary>
    /// <param name="address">The host asked.</param>
    /// <param name="cancellationToken">The token that ends the wait.</param>
    /// <returns>A task that completes once one of them has started.</returns>
    public async Task ReleaseDelayedSwitchAsync(Uri address, CancellationToken cancellationToken)
    {
        using var response = await _client.PostAsync(new Uri(address, "_check/delayed/release"), content: null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Waits until more of a host's delayed switches have settled than <paramref name="count"/>.</summary>
    /// <param name="address">The host asked.</param>
    /// <param name="count">The settled switches already counted.</param>
    /// <param name="cancellationToken">The token that ends the wait.</param>
    /// <returns>A task that completes once another has settled.</returns>
    public async Task WaitForDelayedSwitchAsync(Uri address, int count, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync(new Uri(address, $"_check/delayed/settled?after={count}"), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    // WebKit launches as the first check asking for it opens its page, so a run selecting none never needs it installed.
    private Task<IBrowser> GetWebKitAsync()
    {
        lock (_webKitLock)
        {
            return _webKit ??= _playwright!.Webkit.LaunchAsync(new()
            {
                Headless = Environment.GetEnvironmentVariable("FORMA_BROWSER_VISIBLE") is null,
            });
        }
    }

    /// <summary>The WebView check host the component checks open their pages in under the WebView runtime, with the page of its first web view, <c>A</c>, through which each check opens and closes its own, started as the first of them asks for it and started again as one asks for it once it stopped answering.</summary>
    /// <returns>The application and its first web view's page.</returns>
    /// <remarks>
    /// A check asks for it in its <see cref="WebViewTurn"/>, so an application stopped here has no check left in it. An application stops
    /// answering once its process exits, its browser disconnects, or its first web view, once found, no longer renders interactively; it is
    /// stopped, what it last wrote goes to the output of the check that asked, and another starts in its place. While its first web view
    /// has never been found, as in an application still starting, each check that asks looks for it again.
    /// </remarks>
    public async Task<(WebViewApplication Application, IPage Control)> GetWebViewApplicationAsync()
    {
        var application = await GetStartedWebViewApplicationAsync().ConfigureAwait(false);

        if (!await IsAnsweringAsync(application).ConfigureAwait(false))
        {
            TestContext.Current.TestOutputHelper?.WriteLine($"The WebView check host stopped answering, so another starts in its place. It last wrote:{Environment.NewLine}{application.GetOutput(80)}");

            lock (_webViewLock)
            {
                _webView = null;
                _webViewControl = null;
            }

            await application.DisposeAsync().ConfigureAwait(false);
            application = await GetStartedWebViewApplicationAsync().ConfigureAwait(false);
        }

        _webViewControl ??= await application.GetWebViewAsync("A").ConfigureAwait(false);

        return (application, _webViewControl);
    }

    /// <summary>The Android check host the component checks open their pages in under the android runtime, started as the first of them asks for it.</summary>
    /// <param name="build">The check host's signed package.</param>
    /// <returns>The application.</returns>
    public Task<AndroidCheckApplication> GetAndroidApplicationAsync(string build)
    {
        lock (_androidLock)
        {
            return _android ??= AndroidCheckApplication.StartAsync(build);
        }
    }

    /// <inheritdoc />
    /// <remarks>A host still starting is awaited first, and each part is then stopped however the one before it ends, so a part that fails to stop leaves no host running after the run, nor a publish it holds locked.</remarks>
    public async ValueTask DisposeAsync()
    {
        Task[] hostStarts;

        lock (_hostsLock)
        {
            hostStarts = [.. _hostStarts.Values];
        }

        await Task.WhenAll(hostStarts).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        try
        {
            await DisposeInTurnAsync(
            [
                async () =>
                {
                    if (_webView is { IsCompletedSuccessfully: true } webView)
                    {
                        await (await webView.ConfigureAwait(false)).DisposeAsync().ConfigureAwait(false);
                    }
                },
                async () =>
                {
                    if (_android is { IsCompletedSuccessfully: true } android)
                    {
                        await (await android.ConfigureAwait(false)).DisposeAsync().ConfigureAwait(false);
                    }
                },
                () =>
                {
                    WebViewTurn.Dispose();
                    AndroidTurn.Dispose();

                    return ValueTask.CompletedTask;
                },
                () => Browser?.DisposeAsync() ?? ValueTask.CompletedTask,
                async () =>
                {
                    if (_webKit is { IsCompletedSuccessfully: true } webKit)
                    {
                        await (await webKit.ConfigureAwait(false)).DisposeAsync().ConfigureAwait(false);
                    }
                },
                () =>
                {
                    _playwright?.Dispose();

                    return ValueTask.CompletedTask;
                },
                .. _hosts.Select(host => (Func<ValueTask>)host.DisposeAsync),
            ]).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (_standalone is not null)
                {
                    await _standalone.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _client.Dispose();
            }
        }
    }

    private Task<Uri> StartAsync(string? routes, string? culture, string? requestCulture = null, string? arrangement = null)
        => StartProcessAsync(HostFolder, "Forma.Blazor.Browser.Host.dll", "_check/delayed", new()
        {
            ["FORMA_BROWSER_ROUTES"] = routes,
            ["FORMA_BROWSER_CULTURE"] = culture,
            ["FORMA_BROWSER_REQUEST_CULTURE"] = requestCulture,
            ["FORMA_BROWSER_ARRANGEMENT"] = arrangement,
        });

    // An example host answers its page alone, as a host built from the README does.
    private Task<Uri> StartExampleAsync(string name)
        => StartProcessAsync(Path.Combine(ExamplesFolder, name), $"Forma.Examples.{name}.dll", string.Empty, []);

    // A host starts as the first check asks for it, and a check asking while it starts awaits the same start.
    private Task<Uri> GetStartedHostAsync(string name, Func<Task<Uri>> start)
    {
        lock (_hostsLock)
        {
            if (!_hostStarts.TryGetValue(name, out var hostStart))
            {
                hostStart = Task.Run(start);
                _hostStarts[name] = hostStart;
            }

            return hostStart;
        }
    }

    private async Task<Uri> StartProcessAsync(string folder, string assembly, string readyPath, Dictionary<string, string?> environment)
    {
        var host = await WebHostProcess.StartAsync(folder, assembly, readyPath, environment).ConfigureAwait(false);

        lock (_hostsLock)
        {
            _hosts.Add(host);
        }

        return host.Address;
    }

    // The full client's published files, served as a static host serves a standalone app: each file as it is, and the page for
    // every address that names none.
    private async Task<Uri> StartStandaloneAsync()
    {
        var address = new Uri($"http://127.0.0.1:{LocalPorts.GetFree()}/");
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { WebRootPath = Path.Combine(FullClientFolder, "wwwroot") });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(address.ToString());

        var app = builder.Build();
        app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });
        app.MapFallbackToFile("index.html");
        await app.StartAsync().ConfigureAwait(false);
        _standalone = app;

        return address;
    }

    // The WebView check host running, started where none is.
    private Task<WebViewApplication> GetStartedWebViewApplicationAsync()
    {
        lock (_webViewLock)
        {
            return _webView ??= WebViewCheckHost.StartAsync();
        }
    }

    // Whether the WebView check host still answers: its process runs, its browser is connected, and its first web view, where it was
    // found, still renders interactively.
    private async Task<bool> IsAnsweringAsync(WebViewApplication application)
    {
        if (application.HasStopped)
        {
            return false;
        }

        try
        {
            return _webViewControl is null || await WebViewCheckHost.IsRenderingAsync(_webViewControl, "A").ConfigureAwait(false);
        }
        catch (PlaywrightException)
        {
            return false;
        }
    }

    // Runs each disposal in turn from the one at index, every one of them whatever an earlier one throws.
    private static async ValueTask DisposeInTurnAsync(IReadOnlyList<Func<ValueTask>> disposals, int index = 0)
    {
        if (index == disposals.Count)
        {
            return;
        }

        try
        {
            await disposals[index]().ConfigureAwait(false);
        }
        finally
        {
            await DisposeInTurnAsync(disposals, index + 1).ConfigureAwait(false);
        }
    }

    // Writes the machine's TCP connections to the check's output the first time one of the page's requests fails with failure, the
    // engine's text for a connection the machine refuses once its ports are spent, so the check says what held them.
    private static void WatchConnectionFailures(IPage page, string failure)
    {
        var output = TestContext.Current.TestOutputHelper;
        var hasDescribed = 0;

        page.RequestFailed += (_, request) =>
        {
            if (request.Failure?.Contains(failure, StringComparison.Ordinal) != true || Interlocked.Exchange(ref hasDescribed, 1) == 1)
            {
                return;
            }

            try
            {
                output?.WriteLine($"The request for {request.Url} failed with {request.Failure}. {DescribeTcpConnections()}");
            }
            catch (InvalidOperationException)
            {
                // The check has ended, and its output with it.
            }
        };
    }

    // The machine's TCP connections: how many are in each state, and the remote ports the most of them wait in TIME_WAIT on.
    private static string DescribeTcpConnections()
    {
        var connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
        var states = connections
            .GroupBy(connection => connection.State)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Count()} {group.Key}");
        var waitingPorts = connections
            .Where(connection => connection.State == TcpState.TimeWait)
            .GroupBy(connection => connection.RemoteEndPoint.Port)
            .OrderByDescending(group => group.Count())
            .Take(5)
            .Select(group => $"{group.Count()} on port {group.Key}");

        return $"The machine's TCP connections: {string.Join(", ", states)}; in TIME_WAIT, {string.Join(", ", waitingPorts)}.";
    }

    private static string GetFolder(string variable, string fallback)
    {
        var folder = Path.GetFullPath(Environment.GetEnvironmentVariable(variable) ?? Path.Combine(RepositoryFolder.Find(), fallback));

        return Directory.Exists(folder)
            ? folder
            : throw new InvalidOperationException($"{folder} holds no publish; the gate's browser step publishes it first.");
    }
}
