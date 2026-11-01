namespace Forma.Blazor.Browser.Tests;

/// <summary>What a component's check does in each runtime: opens the component's check page on the components host, waits until the page's runtime is interactive, and waits for the signals a component gives once its script answers input.</summary>
public static partial class ComponentChecks
{
    /// <summary>The runtimes every component is checked in: the Server, WebAssembly, Auto on a first visit, which runs on the Server, and once WebAssembly has downloaded, which runs there, and Blazor Hybrid's web view.</summary>
    public static TheoryData<string> Runtimes { get; } = ["server", "webassembly", "auto-server", "auto-webassembly", "webview"];

    /// <summary>Opens the check page at <paramref name="path"/> in <paramref name="runtime"/> and waits until it is interactive in the runtime the case names.</summary>
    /// <param name="host">The published hosts.</param>
    /// <param name="runtime">The runtime, <c>server</c>, <c>webassembly</c>, <c>auto-server</c>, <c>auto-webassembly</c> or <c>webview</c>.</param>
    /// <param name="path">The component's check page, after the runtime.</param>
    /// <param name="prepare">Prepares the page before it opens the check: a script standing in for what a headless browser lacks, or a route its connections pass through.</param>
    /// <returns>The page and the context that owns it.</returns>
    public static async Task<(IBrowserContext Context, IPage Page)> OpenAsync(BrowserHost host, string runtime, string path, Func<IPage, Task>? prepare = null)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (runtime == "webview")
        {
            return await OpenInWebViewAsync(host, path, prepare).ConfigureAwait(false);
        }

        var (mode, renderer) = runtime switch
        {
            "server" => ("server", "Server"),
            "webassembly" => ("webassembly", "WebAssembly"),
            "auto-server" => ("auto", "Server"),
            "auto-webassembly" => ("auto", "WebAssembly"),
            _ => throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "The runtime is server, webassembly, auto-server, auto-webassembly or webview."),
        };

        var (context, page) = await host.OpenPageAsync(host.ComponentsAddress).ConfigureAwait(false);

        if (prepare is not null)
        {
            await prepare(page).ConfigureAwait(false);
        }

        // Auto runs on WebAssembly once its resources downloaded, so a first visit on the Server downloads them.
        if (runtime == "auto-webassembly")
        {
            var downloads = new FrameworkDownloads(page);
            await page.GotoAsync($"{mode}/{path}").ConfigureAwait(false);
            await WaitForRendererAsync(page, "Server").ConfigureAwait(false);
            await downloads.WaitAsync().ConfigureAwait(false);
            await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => setTimeout(resolve)))").ConfigureAwait(false);
            downloads.Stop();
        }

        await page.GotoAsync($"{mode}/{path}").ConfigureAwait(false);
        await WaitForRendererAsync(page, renderer).ConfigureAwait(false);

        return (context, page);
    }

    /// <summary>Waits until the input router has registered the element <paramref name="selector"/> names, which a component's script does once it answers input.</summary>
    /// <param name="page">The page read.</param>
    /// <param name="selector">The element's selector.</param>
    /// <returns>A task that completes once the router describes the element.</returns>
    public static async Task WaitForInputScopeAsync(IPage page, string selector)
    {
        ArgumentNullException.ThrowIfNull(page);

        await page.EvaluateAsync("async () => { window.formaInputRouter ??= await import(new URL('_content/forma/input/scripts/InputRouter.js', document.baseURI).href); }").ConfigureAwait(false);
        await page.WaitForFunctionAsync("selector => window.formaInputRouter.describe(document.querySelector(selector)) !== null", selector).ConfigureAwait(false);
    }

    /// <summary>Waits until the input router routes <paramref name="action"/> on the element <paramref name="selector"/> names, which the script that answers the action registers.</summary>
    /// <param name="page">The page read.</param>
    /// <param name="selector">The element's selector.</param>
    /// <param name="action">The action's name.</param>
    /// <returns>A task that completes once the router describes the route.</returns>
    public static async Task WaitForInputRouteAsync(IPage page, string selector, string action)
    {
        ArgumentNullException.ThrowIfNull(page);

        await page.EvaluateAsync("async () => { window.formaInputRouter ??= await import(new URL('_content/forma/input/scripts/InputRouter.js', document.baseURI).href); }").ConfigureAwait(false);
        await page.WaitForFunctionAsync("([selector, action]) => window.formaInputRouter.describe(document.querySelector(selector))?.routes.some(route => route.action === action) === true", new[] { selector, action }).ConfigureAwait(false);
    }

    /// <summary>Waits until the input router has placed <paramref name="scope"/> among the page's layers, which it does as an open surface registers.</summary>
    /// <param name="scope">The surface's element.</param>
    /// <returns>A task that completes once the surface carries its layer.</returns>
    public static Task WaitForLayerAsync(ILocator scope)
        => Assertions.Expect(scope).ToHaveAttributeAsync("data-forma-layer", LayerPattern());

    /// <summary>Waits until Blazor's file input listens on the element <paramref name="selector"/> names.</summary>
    /// <param name="page">The page read.</param>
    /// <param name="selector">The file input's selector.</param>
    /// <returns>A task that completes once the input listens.</returns>
    public static Task WaitForFileInputAsync(IPage page, string selector)
    {
        ArgumentNullException.ThrowIfNull(page);

        return page.WaitForFunctionAsync("selector => document.querySelector(selector)?._blazorInputFileNextFileId !== undefined", selector);
    }

    // A check in a web view opens its page in a native window of its own, through the application's first web view, at the size a
    // check's page has in a browser; a page it prepares loads again once prepared, as a browser's page opens prepared. Every web view
    // shares the application's context, so the check's end takes back what it routed and granted, and closes its web view.
    private static async Task<(IBrowserContext Context, IPage Page)> OpenInWebViewAsync(BrowserHost host, string path, Func<IPage, Task>? prepare)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "WebView2 runs on Windows alone, so the web view's checks run in the Windows gate.");

        var application = await host.GetWebViewApplicationAsync().ConfigureAwait(false);
        await host.WebViewTurn.WaitAsync().ConfigureAwait(false);
        var hasEnded = 0;

        async Task EndAsync(IPage control, IPage? page, string? name)
        {
            if (Interlocked.Exchange(ref hasEnded, 1) == 1)
            {
                return;
            }

            try
            {
                if (page is not null && name is not null)
                {
                    var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    page.Close += (_, _) => closed.TrySetResult();
                    await page.Context.UnrouteAllAsync().ConfigureAwait(false);
                    await page.Context.ClearPermissionsAsync().ConfigureAwait(false);
                    await control.Locator("#other").FillAsync(name).ConfigureAwait(false);
                    await control.Locator("#other").BlurAsync().ConfigureAwait(false);
                    await control.Locator("#close-other").ClickAsync().ConfigureAwait(false);

                    if (!page.IsClosed && await Task.WhenAny(closed.Task, Task.Delay(TimeSpan.FromSeconds(30))).ConfigureAwait(false) != closed.Task)
                    {
                        throw new TimeoutException($"The web view {name} didn't close within 30 seconds of its check's end. The check host last wrote:{Environment.NewLine}{application.GetOutput(80)}");
                    }
                }
            }
            finally
            {
                host.WebViewTurn.Release();
            }
        }

        var control = await application.GetWebViewAsync("A").ConfigureAwait(false);
        IPage? page = null;
        string? name = null;

        try
        {
            var opened = control.Locator("#opened");
            var previous = await opened.TextContentAsync().ConfigureAwait(false) ?? string.Empty;
            await control.Locator("#start-path").FillAsync($"/webview/{path}").ConfigureAwait(false);
            await control.Locator("#start-path").BlurAsync().ConfigureAwait(false);
            await control.Locator("#open").ClickAsync().ConfigureAwait(false);
            await Assertions.Expect(opened).Not.ToHaveTextAsync(previous).ConfigureAwait(false);
            name = (await opened.TextContentAsync().ConfigureAwait(false))!;
            page = await application.GetWebViewAsync(name).ConfigureAwait(false);
            await page.SetViewportSizeAsync(1280, 720).ConfigureAwait(false);

            if (prepare is not null)
            {
                await prepare(page).ConfigureAwait(false);
                await page.ReloadAsync().ConfigureAwait(false);
            }

            await WaitForRendererAsync(page, "WebView").ConfigureAwait(false);
        }
        catch
        {
            await EndAsync(control, page, name).ConfigureAwait(false);

            throw;
        }

        return (WebViewCheckContext.Create(page.Context, () => EndAsync(control, page, name)), page);
    }

    private static Task WaitForRendererAsync(IPage page, string renderer)
        => Assertions.Expect(page.Locator("#runtime")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex($"^{renderer} True$"), new() { Timeout = 60000 });

    [System.Text.RegularExpressions.GeneratedRegex(@"^\d+$")]
    private static partial System.Text.RegularExpressions.Regex LayerPattern();
}
