namespace Forma.UI.Features.Composer.Assets;

/// <summary>Loads and caches the browser assets composer plugins declare.</summary>
public interface IComposerAssetLoader : IAsyncDisposable
{
    /// <summary>Loads the asset unless it has already been loaded in this circuit.</summary>
    ValueTask LoadAssetAsync(ComposerAsset asset, CancellationToken cancellationToken = default);

    /// <summary>Loads every asset that has not already been loaded in this circuit.</summary>
    ValueTask LoadAssetsAsync(IEnumerable<ComposerAsset> assets, CancellationToken cancellationToken = default);

    /// <summary>Whether the asset has already been loaded in this circuit.</summary>
    bool IsAssetLoaded(ComposerAsset asset);
}
