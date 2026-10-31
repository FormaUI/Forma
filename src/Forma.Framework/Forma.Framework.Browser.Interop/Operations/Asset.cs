namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>Loads a module, a stylesheet or an SVG sprite once per page.</summary>
public sealed class Asset(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/Asset.js");

    /// <summary>Imports a JavaScript module, reporting whether it loaded.</summary>
    public ValueTask<bool> LoadModuleAsync(string url)
        => _module.TryInvokeAsync<bool>("loadModule", url);

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
