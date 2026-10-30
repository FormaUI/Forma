namespace Forma.Blazor.Browser.Tests;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

/// <summary>The published host the checks drive, running on free ports three ways, and the browser they drive it in.</summary>
/// <remarks>The host runs with islands and culture addresses, with interactive routing and culture addresses, with islands switching the culture state alone, and with the recording router in each runtime. The gate publishes the host and the full client and names their folders in <c>FORMA_BROWSER_HOST</c> and <c>FORMA_BROWSER_FULL</c>; <c>FORMA_BROWSER_VISIBLE</c> opens a visible window instead of a headless one.</remarks>
public sealed class BrowserHost : IAsyncLifetime
{
    private readonly List<Process> _processes = [];
    private readonly HttpClient _client = new();
    private IPlaywright? _playwright;

    /// <summary>The repository the checks run in.</summary>
    public string RepositoryRoot { get; } = FindRepositoryRoot();

    /// <summary>The folder the host was published to.</summary>
    public string HostFolder { get; } = GetFolder("FORMA_BROWSER_HOST", Path.Combine("artifacts", "browser", "host"));

    /// <summary>The folder the full client was published to.</summary>
    public string FullClientFolder { get; } = GetFolder("FORMA_BROWSER_FULL", Path.Combine("artifacts", "browser", "full"));

    /// <summary>The address of the host whose pages are islands under a static router, switching through culture addresses.</summary>
    public Uri Address { get; private set; } = default!;

    /// <summary>The address of the host whose router is interactive on the Server, switching through culture addresses.</summary>
    public Uri InteractiveAddress { get; private set; } = default!;

    /// <summary>The address of the host whose pages are islands, switching the culture state alone.</summary>
    public Uri StateAddress { get; private set; } = default!;

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
        StateAddress = await StartAsync(routes: null, culture: "state").ConfigureAwait(false);
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

    /// <summary>Asks a host how many of its delayed switches have landed, past the browser, which a check may hold offline.</summary>
    /// <param name="address">The host asked.</param>
    /// <returns>The switches landed so far.</returns>
    public async Task<int> GetDelayedSwitchesAsync(Uri address)
        => int.Parse(await _client.GetStringAsync(new Uri(address, "_check/delayed")).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync().ConfigureAwait(false);
        }

        _playwright?.Dispose();

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

    private async Task<Uri> StartAsync(string? routes, string? culture)
    {
        var address = new Uri($"http://127.0.0.1:{GetFreePort()}/");
        var start = new ProcessStartInfo("dotnet", "Forma.Blazor.Browser.Host.dll")
        {
            WorkingDirectory = HostFolder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.Environment["ASPNETCORE_URLS"] = address.ToString().TrimEnd('/');
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["FORMA_BROWSER_ROUTES"] = routes;
        start.Environment["FORMA_BROWSER_CULTURE"] = culture;

        var process = Process.Start(start) ?? throw new InvalidOperationException("The host did not start.");
        _processes.Add(process);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await WaitForHostAsync(process, address).ConfigureAwait(false);

        return address;
    }

    private async Task WaitForHostAsync(Process process, Uri address)
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
                using var response = await _client.GetAsync(new Uri(address, "_check/delayed")).ConfigureAwait(false);

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

        throw new TimeoutException($"The host at {address} did not answer within a minute.");
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string FindRepositoryRoot()
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
