namespace Forma.Framework.Composer.Plugins;

using Forma.Framework.Plugins;

/// <summary>The plugins a composer has, described once and activated per composer.</summary>
public sealed class ComposerDefinition
{
    private readonly PluginRegistry<IComposerPlugin> _registry;

    internal ComposerDefinition(PluginRegistry<IComposerPlugin> registry)
    {
        _registry = registry;
    }

    /// <summary>A definition registering no plugins.</summary>
    public static ComposerDefinition Empty { get; } = new(new PluginRegistry<IComposerPlugin>());

    /// <summary>Creates the definition of a composer's plugins.</summary>
    /// <param name="configure">Registers the editors, views and capabilities the composer has.</param>
    public static ComposerDefinition Create(Action<ComposerPluginBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ComposerPluginBuilder();

        configure(builder);

        return builder.Build();
    }

    /// <summary>Builds this definition's plugins for one composer.</summary>
    /// <param name="services">The host's services.</param>
    /// <remarks>A plugin's constructor takes the other plugins of the scope, and the host's services for whatever no plugin supplies.</remarks>
    /// <exception cref="InvalidOperationException">A plugin takes something nothing can supply, or takes a plugin that takes it back.</exception>
    public PluginScope<IComposerPlugin> Activate(IServiceProvider services)
        => _registry.Activate(services);
}
