namespace Forma.Examples.Smoke.Tests;

using Forma.Tests.Shared;

/// <summary>The example hosts the smoke checks drive, published from Forma's packages: the Web Apps running on free ports, the Blazor Hybrid applications' publishes, and the browser the Web Apps' pages open in.</summary>
/// <remarks>The gate's browser step publishes every example and names their folder in <c>FORMA_BROWSER_EXAMPLES</c>, the Blazor Hybrid ones on Windows alone, the one platform WebView2 runs on; <c>FORMA_BROWSER_VISIBLE</c> opens a visible window instead of a headless one.</remarks>
public sealed class ExampleHosts : IAsyncLifetime
{
    private readonly Dictionary<string, WebHostProcess> _webApps = new(StringComparer.Ordinal);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>The folder each example host was published to a folder of its own in.</summary>
    public string ExamplesFolder { get; } = Path.GetFullPath(Environment.GetEnvironmentVariable("FORMA_BROWSER_EXAMPLES") ?? Path.Combine(RepositoryFolder.Find(), "artifacts", "browser", "examples"));

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (!Directory.Exists(ExamplesFolder))
        {
            throw new InvalidOperationException($"{ExamplesFolder} holds no publish; the gate's browser step publishes it first.");
        }

        try
        {
            // A Web App answers its page alone, as a host built from the README does.
            foreach (var name in (string[])["Coordinated", "Independent"])
            {
                _webApps[name] = await WebHostProcess.StartAsync(Path.Combine(ExamplesFolder, name), $"Forma.Examples.{name}.dll", string.Empty).ConfigureAwait(false);
            }

            _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            _browser = await _playwright.Chromium.LaunchAsync(new()
            {
                Channel = "chrome",
                Headless = Environment.GetEnvironmentVariable("FORMA_BROWSER_VISIBLE") is null,
            }).ConfigureAwait(false);
        }
        catch
        {
            await DisposeAsync().ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>Opens the page of the Web App example <paramref name="name"/> names, in a context of its own.</summary>
    /// <param name="name">The example's name, its project's without the <c>Forma.Examples</c> prefix.</param>
    /// <returns>The page and the context that owns it.</returns>
    public async Task<(IBrowserContext Context, IPage Page)> OpenPageAsync(string name)
    {
        var context = await _browser!.NewContextAsync(new() { BaseURL = _webApps[name].Address.ToString() }).ConfigureAwait(false);
        var page = await context.NewPageAsync().ConfigureAwait(false);
        await page.GotoAsync(string.Empty).ConfigureAwait(false);

        return (context, page);
    }

    /// <summary>Starts the Blazor Hybrid example <paramref name="name"/> names from its publish, skipping the check where WebView2 can't run.</summary>
    /// <param name="name">The example's name, its project's without the <c>Forma.Examples</c> prefix.</param>
    /// <returns>The application, its first web view opening.</returns>
    public Task<WebViewApplication> StartApplicationAsync(string name)
        => WebViewApplication.StartAsync(Path.Combine(ExamplesFolder, name), $"Forma.Examples.{name}");

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_browser is not null)
            {
                await _browser.DisposeAsync().ConfigureAwait(false);
                _browser = null;
            }

            _playwright?.Dispose();
            _playwright = null;
        }
        finally
        {
            foreach (var webApp in _webApps.Values)
            {
                await webApp.DisposeAsync().ConfigureAwait(false);
            }

            _webApps.Clear();
        }
    }
}
