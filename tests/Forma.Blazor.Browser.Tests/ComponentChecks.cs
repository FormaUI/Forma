namespace Forma.Blazor.Browser.Tests;

/// <summary>What a component's check does in each runtime: opens the component's check page on the components host, waits until the page's runtime is interactive, and waits for the signals a component gives once its script answers input.</summary>
public static partial class ComponentChecks
{
    /// <summary>The runtimes every component is checked in, as the check pages' addresses name them.</summary>
    public static TheoryData<string> Runtimes { get; } = ["server", "webassembly", "auto"];

    /// <summary>Opens the check page at <paramref name="path"/> in <paramref name="runtime"/> and waits until it is interactive there, Auto's in whichever runtime it chose.</summary>
    /// <param name="host">The published hosts.</param>
    /// <param name="runtime">The runtime, <c>server</c>, <c>webassembly</c> or <c>auto</c>.</param>
    /// <param name="path">The component's check page, after the runtime.</param>
    /// <returns>The page and the context that owns it.</returns>
    public static async Task<(IBrowserContext Context, IPage Page)> OpenAsync(BrowserHost host, string runtime, string path)
    {
        ArgumentNullException.ThrowIfNull(host);

        var (context, page) = await host.OpenPageAsync(host.ComponentsAddress).ConfigureAwait(false);
        await page.GotoAsync($"{runtime}/{path}").ConfigureAwait(false);

        var interactive = runtime switch
        {
            "server" => "^Server True$",
            "webassembly" => "^WebAssembly True$",
            _ => "True$",
        };
        await Assertions.Expect(page.Locator("#runtime")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex(interactive), new() { Timeout = 60000 }).ConfigureAwait(false);

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

    [System.Text.RegularExpressions.GeneratedRegex(@"^\d+$")]
    private static partial System.Text.RegularExpressions.Regex LayerPattern();
}
