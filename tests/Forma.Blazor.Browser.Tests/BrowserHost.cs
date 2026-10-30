namespace Forma.Blazor.Browser.Tests;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

/// <summary>The published host the checks drive, running on a free port, and the browser they drive it in.</summary>
/// <remarks>The gate publishes the host and the full client and names their folders in <c>FORMA_BROWSER_HOST</c> and <c>FORMA_BROWSER_FULL</c>; <c>FORMA_BROWSER_VISIBLE</c> opens a visible window instead of a headless one.</remarks>
public sealed class BrowserHost : IAsyncLifetime
{
    private Process? _process;
    private IPlaywright? _playwright;

    /// <summary>The repository the checks run in.</summary>
    public string RepositoryRoot { get; } = FindRepositoryRoot();

    /// <summary>The folder the host was published to.</summary>
    public string HostFolder { get; } = GetFolder("FORMA_BROWSER_HOST", Path.Combine("artifacts", "browser", "host"));

    /// <summary>The folder the full client was published to.</summary>
    public string FullClientFolder { get; } = GetFolder("FORMA_BROWSER_FULL", Path.Combine("artifacts", "browser", "full"));

    /// <summary>The address the host answers at.</summary>
    public Uri Address { get; private set; } = default!;

    /// <summary>The browser the checks open their pages in.</summary>
    public IBrowser Browser { get; private set; } = default!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var port = GetFreePort();
        Address = new Uri($"http://127.0.0.1:{port}/");

        var start = new ProcessStartInfo("dotnet", "Forma.Blazor.Browser.Host.dll")
        {
            WorkingDirectory = HostFolder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.Environment["ASPNETCORE_URLS"] = Address.ToString().TrimEnd('/');
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        _process = Process.Start(start) ?? throw new InvalidOperationException("The host did not start.");
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await WaitForHostAsync().ConfigureAwait(false);

        _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        Browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Channel = "chrome",
            Headless = Environment.GetEnvironmentVariable("FORMA_BROWSER_VISIBLE") is null,
        }).ConfigureAwait(false);
    }

    /// <summary>Opens a page in a context of its own, which shares no cache, storage or cookie with another check's.</summary>
    /// <returns>The page and the context that owns it.</returns>
    public async Task<(IBrowserContext Context, IPage Page)> OpenPageAsync()
    {
        var context = await Browser.NewContextAsync(new() { BaseURL = Address.ToString() }).ConfigureAwait(false);
        var page = await context.NewPageAsync().ConfigureAwait(false);

        return (context, page);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync().ConfigureAwait(false);
        }

        _playwright?.Dispose();

        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync().ConfigureAwait(false);
        }

        _process?.Dispose();
    }

    private async Task WaitForHostAsync()
    {
        using var client = new HttpClient();
        var deadline = DateTime.UtcNow.AddMinutes(1);

        while (DateTime.UtcNow < deadline)
        {
            if (_process!.HasExited)
            {
                throw new InvalidOperationException($"The host exited with code {_process.ExitCode} before it answered.");
            }

            try
            {
                using var response = await client.GetAsync(new Uri(Address, "server")).ConfigureAwait(false);

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

        throw new TimeoutException($"The host at {Address} did not answer within a minute.");
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
        var folder = Environment.GetEnvironmentVariable(variable) ?? Path.Combine(FindRepositoryRoot(), fallback);

        return Directory.Exists(folder)
            ? folder
            : throw new InvalidOperationException($"{folder} holds no publish; the gate's browser step publishes it first.");
    }
}
