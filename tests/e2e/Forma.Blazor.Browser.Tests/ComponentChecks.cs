namespace Forma.Blazor.Browser.Tests;

using Forma.Tests.Shared;

/// <summary>What a component's check does in each runtime: opens the component's check page on the components host, waits until the page's runtime is interactive, waits for the signals a component gives once its script answers input, and makes a round trip through the runtime before it reads that something didn't happen.</summary>
public static partial class ComponentChecks
{
    /// <summary>A script recording, in <c>window.formaCopied</c>, each text the page writes to the clipboard once the write lands, so a check reads its own copy rather than a clipboard every browser on the machine shares.</summary>
    public const string RecordingClipboard = "const writeText = Clipboard.prototype.writeText; Clipboard.prototype.writeText = function (text) { return writeText.call(this, text).then(() => { (window.formaCopied ??= []).push(text); }); };";

    /// <summary>The runtimes every component is checked in: the Server, WebAssembly, Auto on a first visit, which runs on the Server, and once WebAssembly has downloaded, which runs there, the Server in WebKit, the engine of Safari and of Apple's web views, and Blazor Hybrid's web view, WebView2's and Android's.</summary>
    /// <remarks>Each row carries its runtime as the <c>Runtime</c> trait, which a run that enumerates theories selects or leaves out by the runner's trait filters.</remarks>
    public static TheoryData<string> Runtimes { get; } = [.. ((string[])["server", "webassembly", "auto-server", "auto-webassembly", "webkit", "webview", "android"]).Select(runtime => new TheoryDataRow<string>(runtime).WithTrait("Runtime", runtime))];

    /// <summary>Every check page's address after its runtime segment, read from the pages' own routes.</summary>
    /// <returns>The addresses, in order.</returns>
    public static IReadOnlyList<string> ReadPagePaths()
        => [.. Directory.EnumerateFiles(Path.Combine(RepositoryFolder.Find(), "tests", "e2e", "Components", "Forma.Blazor.Browser.Components.Checks"), "*.razor", SearchOption.AllDirectories)
            .SelectMany(file => PageRoute().Matches(File.ReadAllText(file)).Select(match => match.Groups["path"].Value))
            .Order(StringComparer.Ordinal)];

    /// <summary>Opens the check page at <paramref name="path"/> in <paramref name="runtime"/> and waits until it is interactive in the runtime the case names.</summary>
    /// <param name="host">The published hosts.</param>
    /// <param name="runtime">The runtime, <c>server</c>, <c>webassembly</c>, <c>auto-server</c>, <c>auto-webassembly</c>, <c>webkit</c>, <c>webview</c> or <c>android</c>.</param>
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

        if (runtime == "android")
        {
            return await OpenOnAndroidAsync(host, path, prepare).ConfigureAwait(false);
        }

        var (mode, renderer) = runtime switch
        {
            "server" => ("server", "Server"),
            "webassembly" => ("webassembly", "WebAssembly"),
            "auto-server" => ("auto", "Server"),
            "auto-webassembly" => ("auto", "WebAssembly"),
            "webkit" => ("server", "Server"),
            _ => throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "The runtime is server, webassembly, auto-server, auto-webassembly, webkit, webview or android."),
        };

        var address = await host.GetComponentsAddressAsync().ConfigureAwait(false);
        var (context, page) = runtime == "webkit"
            ? await host.OpenWebKitPageAsync(address).ConfigureAwait(false)
            : await host.OpenPageAsync(address).ConfigureAwait(false);
        var problems = WatchProblems(page);

        if (prepare is not null)
        {
            await prepare(page).ConfigureAwait(false);
        }

        // Auto runs on WebAssembly once its resources downloaded, so a first visit on the Server downloads them.
        if (runtime == "auto-webassembly")
        {
            var downloads = new FrameworkDownloads(page);
            await page.GotoAsync($"{mode}/{path}").ConfigureAwait(false);
            await WaitForRendererAsync(page, "Server", problems).ConfigureAwait(false);
            await downloads.StopOnceDownloadedAsync().ConfigureAwait(false);
        }

        // WebKit holds the load event while a video it can't fetch anything for stays empty, so there the page is opened once parsed,
        // the wait for its renderer following either way.
        await page.GotoAsync($"{mode}/{path}", new() { WaitUntil = runtime == "webkit" ? WaitUntilState.DOMContentLoaded : WaitUntilState.Load }).ConfigureAwait(false);
        await WaitForRendererAsync(page, renderer, problems).ConfigureAwait(false);

        return (context, page);
    }

    /// <summary>Lets the page write to the clipboard in <paramref name="runtime"/>, as a press asking for a copy may: Chromium's engine asks for the permission, and WebKit lets the press that asks write.</summary>
    /// <param name="context">The page's context.</param>
    /// <param name="runtime">The runtime the page runs in.</param>
    /// <returns>A task that completes once the page may write.</returns>
    public static Task AllowClipboardWriteAsync(IBrowserContext context, string runtime)
    {
        ArgumentNullException.ThrowIfNull(context);

        return runtime == "webkit" ? Task.CompletedTask : context.GrantPermissionsAsync(["clipboard-write"]);
    }

    /// <summary>Waits until <paramref name="item"/> stands in the page's viewport, which a script brings it into, as a list opening on it or scrolling to it does.</summary>
    /// <param name="item">The element.</param>
    /// <returns>A task that completes once the element is in view.</returns>
    /// <remarks>It waits as long as a page's own waits, as every wait on a script's answer here does, since a busy machine can hold a script's first call past an assertion's.</remarks>
    public static Task WaitForInViewportAsync(ILocator item)
        => Assertions.Expect(item).ToBeInViewportAsync(new() { Timeout = 30000 });

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

    /// <summary>Waits until the DOM engine has installed the interaction of <paramref name="capability"/> on the element <paramref name="selector"/> finds, its listeners in place, as a text field's edits are once they listen.</summary>
    /// <param name="page">The page.</param>
    /// <param name="selector">The element's selector.</param>
    /// <param name="capability">The capability, such as <c>edits</c>.</param>
    /// <returns>A task that completes once the interaction listens.</returns>
    public static async Task WaitForInteractionAsync(IPage page, string selector, string capability)
    {
        ArgumentNullException.ThrowIfNull(page);

        await page.EvaluateAsync("async () => { window.formaDomEngine ??= await import(new URL('_content/forma/browser/scripts/DomEngine.js', document.baseURI).href); }").ConfigureAwait(false);
        await page.WaitForFunctionAsync("([selector, capability]) => window.formaDomEngine.describe(document.querySelector(selector)).capabilities.includes(capability)", new[] { selector, capability }).ConfigureAwait(false);
    }

    /// <summary>Waits until the DOM watcher observes <paramref name="capability"/> of the element <paramref name="selector"/> finds, delivering its changes, as a field watching whether the reader is within it does once it observes.</summary>
    /// <param name="page">The page.</param>
    /// <param name="selector">The element's selector.</param>
    /// <param name="capability">The capability, such as <c>focus</c>.</param>
    /// <returns>A task that completes once the observation delivers.</returns>
    public static async Task WaitForObservationAsync(IPage page, string selector, string capability)
    {
        ArgumentNullException.ThrowIfNull(page);

        await page.EvaluateAsync("async () => { window.formaDomWatcher ??= await import(new URL('_content/forma/browser/scripts/DomWatcher.js', document.baseURI).href); }").ConfigureAwait(false);
        await page.WaitForFunctionAsync("([selector, capability]) => window.formaDomWatcher.describe(document.querySelector(selector)).capabilities.includes(capability)", new[] { selector, capability }).ConfigureAwait(false);
    }

    /// <summary>Waits until the input router has placed <paramref name="scope"/> among the page's layers, which it does as an open surface registers.</summary>
    /// <param name="scope">The surface's element.</param>
    /// <returns>A task that completes once the surface carries its layer.</returns>
    /// <remarks>It waits as long as a page's own waits, as every wait on a script's answer here does, since a busy machine can hold a script's first call past an assertion's.</remarks>
    public static Task WaitForLayerAsync(ILocator scope)
        => Assertions.Expect(scope).ToHaveAttributeAsync("data-forma-layer", LayerPattern(), new() { Timeout = 30000 });

    /// <summary>Waits until an input inspector's listeners are attached to <paramref name="inspector"/>, which the inspector marks while they are.</summary>
    /// <param name="inspector">The inspector's element.</param>
    /// <returns>A task that completes once the inspector listens.</returns>
    /// <remarks>It waits as long as a page's own waits, as every wait on a script's answer here does.</remarks>
    public static Task WaitForInspectingAsync(ILocator inspector)
        => Assertions.Expect(inspector).ToHaveAttributeAsync("data-forma-inspecting", string.Empty, new() { Timeout = 30000 });

    /// <summary>Makes a round trip through the runtime rendering the page: a press on the page's round trip, which the runtime handles after every event the page sent before it, and the count it renders in answer.</summary>
    /// <param name="page">The page.</param>
    /// <returns>A task that completes once the runtime has rendered its answer.</returns>
    /// <remarks>The runtime handles the page's events in the order the page sent them, and renders what each handler changed before it takes the next, so once the answer renders, whatever an earlier event did at once has rendered too; a check reads that something did not happen after one. The press is dispatched to the round trip itself, out of sight, so it moves no focus and leaves the page's selection alone.</remarks>
    public static async Task MakeRoundTripAsync(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var roundTrips = page.Locator("#round-trips");
        var made = int.Parse(await roundTrips.TextContentAsync().ConfigureAwait(false) ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        await page.Locator("#round-trip").DispatchEventAsync("click").ConfigureAwait(false);
        await Assertions.Expect(roundTrips).ToHaveTextAsync((made + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(false);
    }

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
    // check's page has in a browser; a page it prepares loads again once prepared, as a browser's page opens prepared, after its first
    // load has started its renderer, since a web view reloaded while its renderer starts can leave it unstarted. Every web view
    // shares the application's context, so the check's end takes back what it routed and granted, and closes its web view. The first
    // web view is found before the check takes its turn, and the check names the web view it opens, so it finds its own however late
    // another opens, and closes it by that name however far the check came; both presses go through the first web view, which takes
    // them in turn, so a close follows the open it closes. Whatever fails on the way, the turn passes on and no native window stays open.
    private static async Task<(IBrowserContext Context, IPage Page)> OpenInWebViewAsync(BrowserHost host, string path, Func<IPage, Task>? prepare)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "WebView2 runs on Windows alone, so the web view's checks run in the Windows gate.");

        var application = await host.GetWebViewApplicationAsync().ConfigureAwait(false);
        var control = await application.GetWebViewAsync("A").ConfigureAwait(false);
        await host.WebViewTurn.WaitAsync().ConfigureAwait(false);
        var name = $"check-{Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture)[..8]}";
        var hasEnded = 0;

        async Task EndAsync(IPage? page)
        {
            if (Interlocked.Exchange(ref hasEnded, 1) == 1)
            {
                return;
            }

            try
            {
                var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                if (page is not null)
                {
                    page.Close += (_, _) => closed.TrySetResult();
                    await page.Context.UnrouteAllAsync().ConfigureAwait(false);
                    await page.Context.ClearPermissionsAsync().ConfigureAwait(false);
                }

                await control.Locator("#other").FillAsync(name).ConfigureAwait(false);
                await control.Locator("#other").BlurAsync().ConfigureAwait(false);
                await control.Locator("#close-other").ClickAsync().ConfigureAwait(false);

                if (page is { IsClosed: false })
                {
                    try
                    {
                        await closed.Task.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                    }
                    catch (TimeoutException exception)
                    {
                        throw new TimeoutException($"The web view {name} didn't close within 30 seconds of its check's end. The check host last wrote:{Environment.NewLine}{application.GetOutput(80)}", exception);
                    }
                }
            }
            finally
            {
                host.WebViewTurn.Release();
            }
        }

        IPage? page = null;

        try
        {
            await control.Locator("#start-path").FillAsync($"/webview/{path}").ConfigureAwait(false);
            await control.Locator("#start-path").BlurAsync().ConfigureAwait(false);
            await control.Locator("#open-as").FillAsync(name).ConfigureAwait(false);
            await control.Locator("#open-as").BlurAsync().ConfigureAwait(false);
            await control.Locator("#open").ClickAsync().ConfigureAwait(false);
            page = await application.GetWebViewAsync(name).ConfigureAwait(false);
            var problems = WatchProblems(page);
            await page.SetViewportSizeAsync(1280, 720).ConfigureAwait(false);

            if (prepare is not null)
            {
                await WaitForRendererAsync(page, "WebView", problems, () => application.GetOutput(80)).ConfigureAwait(false);
                await prepare(page).ConfigureAwait(false);
                await page.ReloadAsync().ConfigureAwait(false);
            }

            await WaitForRendererAsync(page, "WebView", problems, () => application.GetOutput(80)).ConfigureAwait(false);
        }
        catch
        {
            await EndAsync(page).ConfigureAwait(false);

            throw;
        }

        return (WebViewCheckContext.Create(page.Context, () => EndAsync(page)), page);
    }

    // A check on Android opens its page in the check host's one web view, through a connection of its own, at the size a check's page has in
    // a browser, loading the page afresh at its address, which starts the web view's scope anew as a new web view's does; checks take the
    // web view in turn, and closing the connection at the check's end takes back what the check added. An emulator answers a press in
    // seconds, so what a check expects is waited for as long as a script's first answer is, for every check the run holds, which the
    // browser step keeps to Android's. A build named that doesn't exist fails the check rather than skipping it.
    private static async Task<(IBrowserContext Context, IPage Page)> OpenOnAndroidAsync(BrowserHost host, string path, Func<IPage, Task>? prepare)
    {
        var build = AndroidDevice.GetBuild("FORMA_BROWSER_ANDROID_CHECKS");
        Assert.SkipWhen(build is null, "No Android build of the check host is named; the browser step builds one where -Android asks for it and names it in FORMA_BROWSER_ANDROID_CHECKS.");
        Assert.SkipUnless(await AndroidDevice.IsAttachedAsync().ConfigureAwait(false), "No Android device or emulator is attached.");

        Assertions.SetDefaultExpectTimeout(30000);
        var application = await host.GetAndroidApplicationAsync(build).ConfigureAwait(false);
        await host.AndroidTurn.WaitAsync().ConfigureAwait(false);
        IBrowser? browser = null;
        var hasEnded = 0;

        async Task EndAsync()
        {
            if (Interlocked.Exchange(ref hasEnded, 1) == 1)
            {
                return;
            }

            try
            {
                if (browser is not null)
                {
                    await browser.CloseAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                host.AndroidTurn.Release();
            }
        }

        try
        {
            (browser, var page) = await application.ConnectAsync().ConfigureAwait(false);
            var problems = WatchProblems(page);
            await page.SetViewportSizeAsync(1280, 720).ConfigureAwait(false);

            if (prepare is not null)
            {
                await prepare(page).ConfigureAwait(false);
            }

            await page.GotoAsync(new Uri(new Uri(page.Url), $"/android/{path}").ToString()).ConfigureAwait(false);
            await WaitForRendererAsync(page, "WebView", problems).ConfigureAwait(false);

            return (WebViewCheckContext.Create(page.Context, EndAsync), page);
        }
        catch
        {
            await EndAsync().ConfigureAwait(false);

            throw;
        }
    }

    // A page that never becomes interactive fails with what it reported, its errors and the requests that failed, and in a web view with what
    // the check host last wrote, beside the wait's own message.
    private static async Task WaitForRendererAsync(IPage page, string renderer, System.Collections.Concurrent.ConcurrentQueue<string> problems, Func<string>? readHostOutput = null)
    {
        try
        {
            await Assertions.Expect(page.Locator("#runtime")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex($"^{renderer} True$"), new() { Timeout = 60000 }).ConfigureAwait(false);
        }
        catch (PlaywrightException ex) when (!problems.IsEmpty || readHostOutput is not null)
        {
            var reported = problems.IsEmpty ? string.Empty : $"{Environment.NewLine}The page reported:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}";
            var written = readHostOutput is null ? string.Empty : $"{Environment.NewLine}The check host last wrote:{Environment.NewLine}{readHostOutput()}";

            throw new PlaywrightException($"{ex.Message}{reported}{written}", ex);
        }
    }

    // What a page reports going wrong: its console's errors, its uncaught errors and the requests that failed.
    private static System.Collections.Concurrent.ConcurrentQueue<string> WatchProblems(IPage page)
    {
        var problems = new System.Collections.Concurrent.ConcurrentQueue<string>();
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                problems.Enqueue($"console: {message.Text}");
            }
        };
        page.PageError += (_, error) => problems.Enqueue($"error: {error}");
        page.RequestFailed += (_, request) => problems.Enqueue($"request: {request.Url} {request.Failure}");

        return problems;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^\d+$")]
    private static partial System.Text.RegularExpressions.Regex LayerPattern();

    [System.Text.RegularExpressions.GeneratedRegex("""@page "/\{Runtime\}/(?<path>[^"]+)"$""", System.Text.RegularExpressions.RegexOptions.Multiline)]
    private static partial System.Text.RegularExpressions.Regex PageRoute();
}
