namespace Forma.Blazor.Browser.Tests;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>The published host the checks drive, running on free ports in each of its modes, the published full client served as a standalone app, the published components host and example hosts, and the browser they drive them in.</summary>
/// <remarks>The host runs with islands and culture addresses, with interactive routing and culture addresses, with islands switching the culture state alone, with islands persisting a switch in the culture cookie, with islands and culture addresses beside an independent client culture, and with the recording router in each runtime. The gate publishes the host, the full client, the components host and the example hosts and names their folders in <c>FORMA_BROWSER_HOST</c>, <c>FORMA_BROWSER_FULL</c>, <c>FORMA_BROWSER_COMPONENTS</c> and <c>FORMA_BROWSER_EXAMPLES</c>; <c>FORMA_BROWSER_VISIBLE</c> opens a visible window instead of a headless one.</remarks>
public sealed class BrowserHost : IAsyncLifetime
{
    private readonly List<Process> _processes = [];
    private readonly HttpClient _client = new();
    private IPlaywright? _playwright;
    private WebApplication? _standalone;

    /// <summary>The repository the checks run in.</summary>
    public string RepositoryRoot { get; } = FindRepositoryRoot();

    /// <summary>The folder the host was published to.</summary>
    public string HostFolder { get; } = GetFolder("FORMA_BROWSER_HOST", Path.Combine("artifacts", "browser", "host"));

    /// <summary>The folder the full client was published to.</summary>
    public string FullClientFolder { get; } = GetFolder("FORMA_BROWSER_FULL", Path.Combine("artifacts", "browser", "full"));

    /// <summary>The folder the components host was published to.</summary>
    public string ComponentsFolder { get; } = GetFolder("FORMA_BROWSER_COMPONENTS", Path.Combine("artifacts", "browser", "components"));

    /// <summary>The folder each example host was published to a folder of its own in.</summary>
    public string ExamplesFolder { get; } = GetFolder("FORMA_BROWSER_EXAMPLES", Path.Combine("artifacts", "browser", "examples"));

    /// <summary>The address of the host whose pages are islands under a static router, switching through culture addresses.</summary>
    public Uri Address { get; private set; } = default!;

    /// <summary>The address of the host whose router is interactive on the Server, switching through culture addresses.</summary>
    public Uri InteractiveAddress { get; private set; } = default!;

    /// <summary>The address of the host whose pages are islands, switching the culture state alone, which renders every request in <c>en-US</c>.</summary>
    public Uri StateAddress { get; private set; } = default!;

    /// <summary>The address of the host whose pages are islands, switching the culture state alone, which renders every request in the invariant culture.</summary>
    public Uri InvariantStateAddress { get; private set; } = default!;

    /// <summary>The address of the host whose pages are islands, persisting a switch in the culture cookie request localization reads.</summary>
    public Uri CookieAddress { get; private set; } = default!;

    /// <summary>The address of the host whose pages are islands switching through culture addresses, beside a client culture each WebAssembly runtime keeps as its own, with culture capture off.</summary>
    public Uri IndependentAddress { get; private set; } = default!;

    /// <summary>The address the full client is served at as a standalone app, with no server of its own.</summary>
    public Uri StandaloneAddress { get; private set; } = default!;

    /// <summary>The address of the components host, whose check pages each render one component in the runtime their address names.</summary>
    public Uri ComponentsAddress { get; private set; } = default!;

    /// <summary>The address of the example host whose client follows the page's culture.</summary>
    public Uri CoordinatedExampleAddress { get; private set; } = default!;

    /// <summary>The address of the example host whose client's culture is its own.</summary>
    public Uri IndependentExampleAddress { get; private set; } = default!;

    /// <summary>The address of the host rendering the recording router on the Server.</summary>
    public Uri NavigationServerAddress { get; private set; } = default!;

    /// <summary>The address of the host rendering the recording router under WebAssembly.</summary>
    public Uri NavigationWebAssemblyAddress { get; private set; } = default!;

    /// <summary>The browser the checks open their pages in.</summary>
    public IBrowser Browser { get; private set; } = default!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Address = await StartAsync(routes: null, culture: null).ConfigureAwait(false);
        InteractiveAddress = await StartAsync(routes: "interactive", culture: null).ConfigureAwait(false);
        StateAddress = await StartAsync(routes: null, culture: "state", requestCulture: "en-US").ConfigureAwait(false);
        InvariantStateAddress = await StartAsync(routes: null, culture: "state", requestCulture: "invariant").ConfigureAwait(false);
        CookieAddress = await StartAsync(routes: null, culture: "cookie").ConfigureAwait(false);
        IndependentAddress = await StartAsync(routes: null, culture: null, arrangement: "independent").ConfigureAwait(false);
        StandaloneAddress = await StartStandaloneAsync().ConfigureAwait(false);
        ComponentsAddress = await StartProcessAsync(ComponentsFolder, "Forma.Blazor.Browser.Components.dll", "_check/ready", []).ConfigureAwait(false);
        CoordinatedExampleAddress = await StartExampleAsync("Coordinated").ConfigureAwait(false);
        IndependentExampleAddress = await StartExampleAsync("Independent").ConfigureAwait(false);
        NavigationServerAddress = await StartAsync(routes: "navigation-server", culture: null).ConfigureAwait(false);
        NavigationWebAssemblyAddress = await StartAsync(routes: "navigation-webassembly", culture: null).ConfigureAwait(false);

        _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        Browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Channel = "chrome",
            Headless = Environment.GetEnvironmentVariable("FORMA_BROWSER_VISIBLE") is null,
        }).ConfigureAwait(false);
    }

    /// <summary>Opens a page in a context of its own, which shares no cache, storage or cookie with another check's.</summary>
    /// <param name="address">The host the page's relative addresses resolve against; the islands host where none is given.</param>
    /// <returns>The page and the context that owns it.</returns>
    public async Task<(IBrowserContext Context, IPage Page)> OpenPageAsync(Uri? address = null)
    {
        var context = await Browser.NewContextAsync(new() { BaseURL = (address ?? Address).ToString() }).ConfigureAwait(false);
        var page = await context.NewPageAsync().ConfigureAwait(false);

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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync().ConfigureAwait(false);
        }

        _playwright?.Dispose();

        if (_standalone is not null)
        {
            await _standalone.DisposeAsync().ConfigureAwait(false);
        }

        foreach (var process in _processes)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().ConfigureAwait(false);
            }

            process.Dispose();
        }

        _client.Dispose();
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

    private async Task<Uri> StartProcessAsync(string folder, string assembly, string readyPath, Dictionary<string, string?> environment)
    {
        var address = new Uri($"http://127.0.0.1:{GetFreePort()}/");
        var start = new ProcessStartInfo("dotnet", assembly)
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.Environment["ASPNETCORE_URLS"] = address.ToString().TrimEnd('/');
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        foreach (var (name, value) in environment)
        {
            start.Environment[name] = value;
        }

        var process = Process.Start(start) ?? throw new InvalidOperationException($"{assembly} did not start.");
        _processes.Add(process);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await WaitForHostAsync(process, new Uri(address, readyPath)).ConfigureAwait(false);

        return address;
    }

    // The full client's published files, served as a static host serves a standalone app: each file as it is, and the page for
    // every address that names none.
    private async Task<Uri> StartStandaloneAsync()
    {
        var address = new Uri($"http://127.0.0.1:{GetFreePort()}/");
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

    private async Task WaitForHostAsync(Process process, Uri ready)
    {
        var deadline = DateTime.UtcNow.AddMinutes(1);

        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The host exited with code {process.ExitCode} before it answered.");
            }

            try
            {
                using var response = await _client.GetAsync(ready).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Not listening yet; the next attempt asks again.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
        }

        throw new TimeoutException($"The host at {ready} did not answer within a minute.");
    }

    /// <summary>A port nothing on this machine listens on now.</summary>
    /// <returns>The port.</returns>
    internal static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    /// <summary>The repository the checks run in, found above the checks' own folder.</summary>
    /// <returns>The repository's folder.</returns>
    internal static string FindRepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (folder.GetFiles("*.slnx").Length > 0)
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("The checks run from outside the repository.");
    }

    private static string GetFolder(string variable, string fallback)
    {
        var folder = Path.GetFullPath(Environment.GetEnvironmentVariable(variable) ?? Path.Combine(FindRepositoryRoot(), fallback));

        return Directory.Exists(folder)
            ? folder
            : throw new InvalidOperationException($"{folder} holds no publish; the gate's browser step publishes it first.");
    }
}
