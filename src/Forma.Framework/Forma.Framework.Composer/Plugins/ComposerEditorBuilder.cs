namespace Forma.Framework.Composer.Plugins;

using Forma.Framework.Plugins;

/// <summary>Registers the plugins and views that belong to one editor.</summary>
/// <typeparam name="TEditor">The editor being described.</typeparam>
public sealed class ComposerEditorBuilder<TEditor>
    where TEditor : class, IComposerEditorPlugin
{
    private readonly PluginRegistry<IComposerPlugin> _registry;

    internal ComposerEditorBuilder(PluginRegistry<IComposerPlugin> registry)
    {
        _registry = registry;
    }

    /// <summary>Registers a capability on this editor.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    public ComposerEditorBuilder<TEditor> AddFeaturePlugin<TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _registry.Add<TPlugin>();

        return this;
    }

    /// <summary>Registers a capability on this editor, configuring the options it takes.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    /// <typeparam name="TOptions">The options record the plugin declares through <see cref="IPluginConfigurable{TOptions}"/>.</typeparam>
    /// <param name="configure">Fills the options record created for this registration.</param>
    public ComposerEditorBuilder<TEditor> AddFeaturePlugin<TPlugin, TOptions>(Action<TOptions> configure)
        where TPlugin : class, IComposerFeaturePlugin, IPluginConfigurable<TOptions>
        where TOptions : class, new()
    {
        _registry.Add<TPlugin, TOptions>(configure);

        return this;
    }

    /// <summary>Registers a capability on this editor, created through the given factory.</summary>
    /// <typeparam name="TPlugin">The plugin the factory creates.</typeparam>
    /// <param name="factory">Creates the plugin for one composer, from the host's services.</param>
    public ComposerEditorBuilder<TEditor> AddFeaturePlugin<TPlugin>(Func<IServiceProvider, TPlugin> factory)
        where TPlugin : class, IComposerFeaturePlugin
    {
        _registry.Add(factory);

        return this;
    }

    /// <summary>Registers a capability on this editor unless the editor already registers it itself.</summary>
    /// <typeparam name="TPlugin">The plugin to fall back on.</typeparam>
    public ComposerEditorBuilder<TEditor> TryAddFeaturePlugin<TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _registry.TryAdd<TPlugin>();

        return this;
    }

    /// <summary>Registers a read-only view over this editor's document, and the plugins that belong to it.</summary>
    /// <typeparam name="TPlugin">The view to create.</typeparam>
    /// <param name="configure">Registers the view's own plugins.</param>
    public ComposerEditorBuilder<TEditor> AddViewPlugin<TPlugin>(Action<ComposerViewBuilder>? configure = null)
        where TPlugin : class, IComposerViewPlugin
    {
        _registry.Add<TPlugin>(view => configure?.Invoke(new ComposerViewBuilder(view)));

        return this;
    }
}
