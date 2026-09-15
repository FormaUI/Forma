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

    /// <summary>A description registering nothing, which leaves a composer with the editor it falls back on.</summary>
    public static ComposerDefinition Empty { get; } = new(new PluginRegistry<IComposerPlugin>());

    /// <summary>Describes a composer's plugins.</summary>
    /// <param name="configure">Registers the editors, views and capabilities the composer has.</param>
    public static ComposerDefinition Create(Action<ComposerPluginBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ComposerPluginBuilder();

        configure(builder);

        return builder.Build();
    }

    /// <summary>Builds this description's plugins for one composer, which disposes the scope when it is done with them.</summary>
    /// <param name="services">The host's services, which stand in for whatever a plugin's constructor asks for and no plugin supplies.</param>
    /// <exception cref="InvalidOperationException">A plugin takes something nothing can supply, or takes a plugin that takes it back.</exception>
    public PluginScope<IComposerPlugin> Activate(IServiceProvider services)
        => _registry.Activate(services);
}
