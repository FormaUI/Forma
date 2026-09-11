namespace Forma.Blazor.Components.Composer.Assets;

using Forma.Framework.Common.Interop.Operations;
using Forma.Framework.Composer.Assets;
using Forma.Framework.Core.Interop;

/// <summary>Loads browser assets a plugin declares, caching what has already loaded per circuit.</summary>
internal sealed class JSComposerAssetLoader(IJSEnvironment js) : IComposerAssetLoader
{
    private readonly Asset _asset = new(js);
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
            ComposerAssetKind.Module => await _asset.LoadModuleAsync(asset.Url),
            _ => await _asset.LoadStyleAsync(asset.Url),
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
        await _asset.DisposeAsync();
    }
}
