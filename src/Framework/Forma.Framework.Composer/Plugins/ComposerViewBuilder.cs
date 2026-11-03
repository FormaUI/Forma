namespace Forma.Framework.Composer.Plugins;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Plugins;

/// <summary>Registers the plugins that belong to one view over an editor's document.</summary>
public sealed class ComposerViewBuilder
{
    private readonly PluginRegistry<IComposerPlugin> _registry;

    internal ComposerViewBuilder(PluginRegistry<IComposerPlugin> registry)
    {
        _registry = registry;
    }

    /// <summary>Registers a capability on this view.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    public ComposerViewBuilder AddFeaturePlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _registry.Add<TPlugin>();

        return this;
    }

    /// <summary>Registers a capability on this view, configuring the options it takes.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    /// <typeparam name="TOptions">The options record the plugin declares through <see cref="IPluginConfigurable{TOptions}"/>.</typeparam>
    /// <param name="configure">Fills the options record created for this registration.</param>
    public ComposerViewBuilder AddFeaturePlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin, TOptions>(Action<TOptions> configure)
        where TPlugin : class, IComposerFeaturePlugin, IPluginConfigurable<TOptions>
        where TOptions : class, new()
    {
        _registry.Add<TPlugin, TOptions>(configure);

        return this;
    }

    /// <summary>Registers a capability on this view, created through the given factory.</summary>
    /// <typeparam name="TPlugin">The plugin the factory creates.</typeparam>
    /// <param name="factory">Creates the plugin for one composer, from the host's services.</param>
    public ComposerViewBuilder AddFeaturePlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin>(Func<IServiceProvider, TPlugin> factory)
        where TPlugin : class, IComposerFeaturePlugin
    {
        _registry.Add(factory);

        return this;
    }

    /// <summary>Registers a capability on this view unless the view already registers it itself.</summary>
    /// <typeparam name="TPlugin">The plugin to fall back on.</typeparam>
    public ComposerViewBuilder TryAddFeaturePlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _registry.TryAdd<TPlugin>();

        return this;
    }
}
