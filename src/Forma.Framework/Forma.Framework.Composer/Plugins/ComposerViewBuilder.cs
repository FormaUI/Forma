namespace Forma.Framework.Composer.Plugins;

/// <summary>Registers the plugins that belong to one view over an editor's document.</summary>
public sealed class ComposerViewBuilder
{
    private readonly ComposerPluginScope _scope;

    internal ComposerViewBuilder(ComposerPluginRegistration view)
    {
        _scope = new ComposerPluginScope(view);
    }

    /// <summary>Registers a capability on this view.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    public ComposerViewBuilder AddFeaturePlugin<TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _scope.Add(typeof(TPlugin), optionsType: null, configure: null);

        return this;
    }

    /// <summary>Registers a capability on this view, configuring the options it takes.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    /// <typeparam name="TOptions">The options record the plugin declares through <see cref="IComposerConfigurable{TOptions}"/>.</typeparam>
    /// <param name="configure">Fills the options record created for this registration.</param>
    public ComposerViewBuilder AddFeaturePlugin<TPlugin, TOptions>(Action<TOptions> configure)
        where TPlugin : class, IComposerFeaturePlugin, IComposerConfigurable<TOptions>
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(configure);
        _scope.Add(typeof(TPlugin), typeof(TOptions), options => configure((TOptions)options));

        return this;
    }

    /// <summary>Registers a capability on this view unless the view already registers it itself.</summary>
    /// <typeparam name="TPlugin">The plugin to fall back on.</typeparam>
    public ComposerViewBuilder TryAddFeaturePlugin<TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _scope.TryAdd(typeof(TPlugin));

        return this;
    }

    internal IEnumerable<ComposerPluginRegistration> Collect()
        => _scope.Resolve();
}
