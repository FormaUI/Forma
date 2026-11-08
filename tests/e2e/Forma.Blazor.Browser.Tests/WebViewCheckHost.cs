namespace Forma.Blazor.Browser.Tests;

using Forma.Tests.Shared;

/// <summary>The WebView check host, the Blazor Hybrid application whose native windows each render the components to a web view named for the order it opened in.</summary>
/// <remarks>The gate publishes it on Windows, the one platform WebView2 runs on, and names its folder in <c>FORMA_BROWSER_WEBVIEW</c>.</remarks>
public static class WebViewCheckHost
{
    /// <summary>Starts the check host, skipping the check where WebView2 can't run.</summary>
    /// <param name="environment">The environment variables the check host starts with beside its own, by name.</param>
    /// <returns>The application, its first web view <c>A</c> opening.</returns>
    public static Task<WebViewApplication> StartAsync(IReadOnlyDictionary<string, string>? environment = null)
        => WebViewApplication.StartAsync(Environment.GetEnvironmentVariable("FORMA_BROWSER_WEBVIEW") ?? Path.Combine(RepositoryFolder.Find(), "artifacts", "browser", "webview"), "Forma.Blazor.Browser.WebView", environment);

    /// <summary>Finds the page of the check host's web view <paramref name="name"/> names, once it renders interactively.</summary>
    /// <param name="application">The check host.</param>
    /// <param name="name">The web view's name, <c>A</c> for the first.</param>
    /// <returns>The web view's page.</returns>
    public static Task<IPage> GetWebViewAsync(this WebViewApplication application, string name)
    {
        ArgumentNullException.ThrowIfNull(application);

        return application.FindWebViewAsync(
            async page => await page.EvaluateAsync<string>("() => `${document.querySelector('#web-view')?.textContent}|${document.querySelector('#runtime')?.textContent}`").ConfigureAwait(false) == $"{name}|WebView True",
            $"named {name} rendered interactively",
            TimeSpan.FromSeconds(30));
    }

    /// <summary>Opens the check host's second web view, <c>B</c>, from its first, <c>A</c>, once both render interactively.</summary>
    /// <param name="application">The check host.</param>
    /// <returns>The two web views' pages, the first's ahead.</returns>
    public static async Task<(IPage First, IPage Second)> OpenTwoWebViewsAsync(this WebViewApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        var first = await application.GetWebViewAsync("A").ConfigureAwait(false);
        await first.Locator("#open").ClickAsync().ConfigureAwait(false);
        var second = await application.GetWebViewAsync("B").ConfigureAwait(false);

        return (first, second);
    }
}
