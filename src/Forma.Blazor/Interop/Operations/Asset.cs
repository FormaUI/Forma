namespace Forma.Blazor.Interop.Operations;

using Microsoft.JSInterop;

/// <summary>Loads a script, module, or stylesheet once per page.</summary>
internal sealed class Asset(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.Blazor/scripts/operations/Asset.js");

    /// <summary>Imports a JavaScript module, reporting whether it loaded.</summary>
    public ValueTask<bool> LoadModuleAsync(string url)
        => _module.TryInvokeAsync<bool>("loadModule", url);

    /// <summary>Loads an external script, reporting whether it loaded.</summary>
    public ValueTask<bool> LoadScriptAsync(string url)
        => _module.TryInvokeAsync<bool>("loadScript", url);

    /// <summary>Loads a stylesheet, reporting whether it loaded.</summary>
    public ValueTask<bool> LoadStyleAsync(string url)
        => _module.TryInvokeAsync<bool>("loadStyle", url);

    /// <summary>Adopts an SVG sprite's symbols into the document, reporting whether they arrived.</summary>
    public ValueTask<bool> LoadSpriteAsync(string url)
        => _module.TryInvokeAsync<bool>("loadSprite", url);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
