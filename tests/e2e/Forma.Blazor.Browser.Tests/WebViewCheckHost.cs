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
    public static async Task<IPage> GetWebViewAsync(this WebViewApplication application, string name)
    {
        ArgumentNullException.ThrowIfNull(application);

        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            // A snapshot, since a web view opening meanwhile adds its page to these lists.
            foreach (var page in application.Browser.Contexts.ToArray().SelectMany(context => context.Pages.ToArray()))
            {
                try
                {
                    if (await page.EvaluateAsync<string?>("() => document.querySelector('#web-view')?.textContent ?? null").ConfigureAwait(false) == name)
                    {
                        await Assertions.Expect(page.Locator("#runtime")).ToHaveTextAsync("WebView True").ConfigureAwait(false);

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
}
