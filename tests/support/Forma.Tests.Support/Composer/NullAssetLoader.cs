namespace Forma.Tests.Composer;

using Forma.Framework.Composer.Assets;

/// <summary>An asset loader whose every load completes at once and whose every asset reads as unloaded.</summary>
public sealed class NullAssetLoader : IComposerAssetLoader
{
    /// <inheritdoc />
    public bool IsAssetLoaded(ComposerAsset asset)
        => false;

    /// <inheritdoc />
    public ValueTask LoadAssetAsync(ComposerAsset asset, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask LoadAssetsAsync(IEnumerable<ComposerAsset> assets, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}
