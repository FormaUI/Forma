namespace Forma.UI.Blazor.Components.Composer.Assets;

using Forma.UI.Features.Composer.Assets;

using Microsoft.JSInterop;

using AssetOperations = Interop.Operations.Assets;

/// <summary>Loads browser assets a plugin declares, caching what has already loaded per circuit.</summary>
internal sealed class JsComposerAssetLoader(IJSRuntime js) : IComposerAssetLoader
{
    private readonly AssetOperations _assets = new(js);
    private readonly HashSet<string> _loadedUrls = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async ValueTask LoadAssetAsync(ComposerAsset asset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (_loadedUrls.Contains(asset.Url))
        {
            return;
        }

        // A font is declared through a stylesheet, so both reach the browser the same way.
        var loaded = asset.Kind switch
        {
            ComposerAssetKind.Module => await _assets.LoadModuleAsync(asset.Url),
            _ => await _assets.LoadStyleAsync(asset.Url),
        };

        if (loaded)
        {
            _loadedUrls.Add(asset.Url);
        }
    }

    /// <inheritdoc />
    public async ValueTask LoadAssetsAsync(IEnumerable<ComposerAsset> assets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);

        foreach (var asset in assets)
        {
            await LoadAssetAsync(asset, cancellationToken);
        }
    }

    /// <inheritdoc />
    public bool IsAssetLoaded(ComposerAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return _loadedUrls.Contains(asset.Url);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _loadedUrls.Clear();
        await _assets.DisposeAsync();
    }
}
