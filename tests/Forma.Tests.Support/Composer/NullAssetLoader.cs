namespace Forma.Tests.Composer;

using Forma.UI.Features.Composer.Assets;

/// <summary>An asset loader that reports nothing loaded and performs no loading.</summary>
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
