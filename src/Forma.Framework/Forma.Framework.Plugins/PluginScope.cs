namespace Forma.Framework.Plugins;

/// <summary>One owner's constructed plugins, and the disposal of whichever of them it created.</summary>
/// <typeparam name="TPlugin">The plugin family this holds.</typeparam>
/// <remarks>
/// Being a plugin's owner says nothing about disposing it: the tree and the disposal are separate questions,
/// so a plugin registered under another may still govern its own lifetime, and one at the root may not.
/// </remarks>
public sealed class PluginScope<TPlugin> : IAsyncDisposable
    where TPlugin : class, IPlugin
{
    private bool _isDisposed;

    internal PluginScope(IReadOnlyList<PluginInstance<TPlugin>> instances)
    {
        Instances = instances;
        Plugins = [.. instances.Select(instance => instance.Plugin)];
    }

    /// <summary>A scope holding nothing, fresh each time, since disposing one must not mark another disposed.</summary>
    public static PluginScope<TPlugin> Empty => new([]);

    /// <summary>The plugins with what each was registered under and who governs its lifetime, in registration order.</summary>
    public IReadOnlyList<PluginInstance<TPlugin>> Instances { get; }

    /// <summary>The plugins alone, in registration order.</summary>
    public IReadOnlyList<TPlugin> Plugins { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var instance in Instances.Where(instance => !instance.IsAutonomous))
        {
            switch (instance.Plugin)
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
