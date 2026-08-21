namespace Forma.UI.Blazor.Components.Composer.Assets;

using Forma.UI.Blazor.Interop;
using Forma.UI.Features.Composer.Assets;

/// <summary>Loads browser assets via the composer JS interop module and caches them per circuit.</summary>
internal sealed class JsComposerAssetLoader(JsModule module) : IComposerAssetLoader
{
    private readonly HashSet<string> _loadedUrls = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async ValueTask LoadAssetAsync(ComposerAsset asset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (_loadedUrls.Contains(asset.Url))
        {
            return;
        }

        var loaded = await module.TryInvokeAsync<bool>("loadAsset", (int)asset.Kind, asset.Url);

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
    public ValueTask DisposeAsync()
    {
        _loadedUrls.Clear();
        return ValueTask.CompletedTask;
    }
}
