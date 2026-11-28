namespace Forma.Tests.Hosts;

using Microsoft.Playwright;

/// <summary>The pages of a browser connected over Chrome's DevTools protocol, as Playwright last reported them opening and closing.</summary>
/// <remarks>
/// Playwright adds and removes a browser's pages on the thread that reads the browser's messages, in lists that take no lock, so the
/// tracker reads those lists on that thread alone, in the handlers of what Playwright reports there: each page it adds or closes, and each
/// target the browser announces as tracking starts, whose messages Playwright reads after those of every page it added before. A browser
/// connected over the protocol keeps the contexts it connected with.
/// </remarks>
internal sealed class PageTracker
{
    private readonly IBrowser _browser;
    private IReadOnlyList<IPage> _pages = [];

    private PageTracker(IBrowser browser)
    {
        _browser = browser;
    }

    /// <summary>The browser's pages, as Playwright last reported them.</summary>
    public IReadOnlyList<IPage> Pages
        => Volatile.Read(ref _pages);

    /// <summary>Starts tracking the pages of <paramref name="browser"/>, the pages it holds now among them.</summary>
    /// <param name="browser">The browser, connected over Chrome's DevTools protocol.</param>
    /// <returns>The tracker, holding the pages the browser holds now.</returns>
    public static async Task<PageTracker> StartAsync(IBrowser browser)
    {
        ArgumentNullException.ThrowIfNull(browser);

        var tracker = new PageTracker(browser);

        foreach (var context in browser.Contexts)
        {
            context.Page += (_, _) => tracker.Record();
            context.PageClose += (_, _) => tracker.Record();
        }

        // The browser announces each target it holds before it answers the request to announce them.
        var session = await browser.NewBrowserCDPSessionAsync().ConfigureAwait(false);

        try
        {
            session.Event("Target.targetCreated").OnEvent += (_, _) => tracker.Record();
            await session.SendAsync("Target.setDiscoverTargets", new() { ["discover"] = true }).ConfigureAwait(false);
        }
        finally
        {
            await session.DetachAsync().ConfigureAwait(false);
        }

        return tracker;
    }

    private void Record()
        => Volatile.Write(ref _pages, [.. _browser.Contexts.SelectMany(context => context.Pages)]);
}
