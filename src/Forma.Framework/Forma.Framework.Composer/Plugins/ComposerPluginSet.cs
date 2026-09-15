namespace Forma.Framework.Composer.Plugins;

/// <summary>One composer's plugins, and the disposal of whichever of them it built itself.</summary>
/// <remarks>
/// A plugin a registration supplied through a factory belongs to whoever wrote the factory, so it is left
/// alone; disposing it would take it from the next composer the factory hands it to.
/// </remarks>
public sealed class ComposerPluginSet : IAsyncDisposable
{
    private readonly IReadOnlyList<IComposerPlugin> _owned;
    private bool _isDisposed;

    internal ComposerPluginSet(IReadOnlyList<IComposerPlugin> plugins, IReadOnlyList<IComposerPlugin> owned)
    {
        Plugins = plugins;
        _owned = owned;
    }

    /// <summary>A set holding nothing, fresh each time, since disposing one must not mark another disposed.</summary>
    public static ComposerPluginSet Empty => new([], []);

    /// <summary>The plugins, in the order they were registered.</summary>
    public IReadOnlyList<IComposerPlugin> Plugins { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var plugin in _owned)
        {
            switch (plugin)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;

                case IDisposable disposable:
                    disposable.Dispose();
                    break;

                default:
                    break;
            }
        }
    }
}
