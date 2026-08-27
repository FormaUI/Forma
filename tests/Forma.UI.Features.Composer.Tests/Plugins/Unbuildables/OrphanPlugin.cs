namespace Forma.UI.Features.Composer.Tests.Plugins.Unbuildables;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Plugins;

/// <summary>A feature plugin taking something no plugin can stand in for, which discovery must report.</summary>
public sealed class OrphanPlugin(TimeProvider clock) : IComposerFeaturePlugin
{
    public TimeProvider Clock { get; } = clock;
    public string Id => "orphan";
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
}
