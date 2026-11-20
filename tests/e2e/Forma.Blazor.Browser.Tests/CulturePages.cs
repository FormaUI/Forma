namespace Forma.Blazor.Browser.Tests;

/// <summary>What the culture checks read off a page: the document's language and direction, and whether a panel turned interactive.</summary>
public static class CulturePages
{
    /// <summary>The document's language and direction, as <c>he/rtl</c>.</summary>
    /// <param name="page">The page read.</param>
    /// <returns>The language and the direction.</returns>
    public static Task<string> GetDocumentAsync(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return page.EvaluateAsync<string>("() => `${document.documentElement.lang}/${document.documentElement.dir}`");
    }

    /// <summary>Waits until the document's language and direction are <paramref name="document"/>.</summary>
    /// <param name="page">The page read.</param>
    /// <param name="document">The language and the direction, as <c>he/rtl</c>.</param>
    /// <param name="timeout">How long to wait, in milliseconds.</param>
    /// <returns>A task that completes once the document holds them.</returns>
    public static async Task WaitForDocumentAsync(IPage page, string document, float timeout = 15000)
    {
        ArgumentNullException.ThrowIfNull(page);

        try
        {
            await page.WaitForFunctionAsync("expected => `${document.documentElement.lang}/${document.documentElement.dir}` === expected", document, new() { Timeout = timeout }).ConfigureAwait(false);
        }
        catch (TimeoutException exception)
        {
            throw new TimeoutException($"The document didn't read {document} within {timeout} ms: {await DescribeDocumentAsync(page).ConfigureAwait(false)}", exception);
        }
    }

    // The document's language and direction, and the UI culture of the marker it started from, which a page whose coordinator never
    // started lacks.
    private static async Task<string> DescribeDocumentAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<string>("() => `it reads ${document.documentElement.lang}/${document.documentElement.dir}, and its culture marker names ${document.head.querySelector('meta[name=\"forma-culture\"]')?.getAttribute('data-ui-culture') ?? 'nothing, since the page carries none'}`").ConfigureAwait(false);
        }
        catch (PlaywrightException exception)
        {
            return $"it couldn't be read: {exception.Message}";
        }
    }

    /// <summary>Waits until the Server's culture panel on the page is interactive.</summary>
    /// <param name="page">The page read.</param>
    /// <returns>A task that completes once the panel is interactive.</returns>
    public static async Task WaitForPanelAsync(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        await Assertions.Expect(page.Locator("#interactive")).ToHaveTextAsync("True", new() { Timeout = 30000 }).ConfigureAwait(false);
    }
}
