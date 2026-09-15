namespace Forma.Framework.Composer.Plugins;

/// <summary>Registers the plugins and views that belong to one editor.</summary>
public sealed class ComposerEditorBuilder
{
    private readonly ComposerPluginScope _scope;

    internal ComposerEditorBuilder(ComposerPluginRegistration editor)
    {
        _scope = new ComposerPluginScope(editor);
    }

    /// <summary>Registers a capability on this editor.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    public ComposerEditorBuilder AddFeaturePlugin<TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _scope.Add(typeof(TPlugin), optionsType: null, configure: null);

        return this;
    }

    /// <summary>Registers a capability on this editor, configuring the options it takes.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    /// <typeparam name="TOptions">The options record the plugin declares through <see cref="IComposerConfigurable{TOptions}"/>.</typeparam>
    /// <param name="configure">Fills the options record created for this registration.</param>
    public ComposerEditorBuilder AddFeaturePlugin<TPlugin, TOptions>(Action<TOptions> configure)
        where TPlugin : class, IComposerFeaturePlugin, IComposerConfigurable<TOptions>
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(configure);
        _scope.Add(typeof(TPlugin), typeof(TOptions), options => configure((TOptions)options));

        return this;
    }

    /// <summary>Registers a capability this editor creates through the given factory rather than from its constructor.</summary>
    /// <typeparam name="TPlugin">The plugin the factory creates.</typeparam>
    /// <param name="factory">Creates the plugin for one composer, from the host's services.</param>
    /// <remarks>A factory answering the same instance every time hands that instance to every composer.</remarks>
    public ComposerEditorBuilder AddFeaturePlugin<TPlugin>(Func<IServiceProvider, TPlugin> factory)
        where TPlugin : class, IComposerFeaturePlugin
    {
        ArgumentNullException.ThrowIfNull(factory);
        _scope.Add(typeof(TPlugin), factory: services => factory(services));

        return this;
    }

    /// <summary>Registers a capability on this editor unless the editor already registers it itself.</summary>
    /// <typeparam name="TPlugin">The plugin to fall back on.</typeparam>
    public ComposerEditorBuilder TryAddFeaturePlugin<TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _scope.TryAdd(typeof(TPlugin));

        return this;
    }

    /// <summary>Registers a read-only view over this editor's document.</summary>
    /// <typeparam name="TPlugin">The view to create.</typeparam>
    public ComposerEditorBuilder AddViewPlugin<TPlugin>()
        where TPlugin : class, IComposerViewPlugin
        => AddViewPlugin<TPlugin>(_ => { });

    /// <summary>Registers a read-only view over this editor's document, and the plugins that belong to it.</summary>
    /// <typeparam name="TPlugin">The view to create.</typeparam>
    /// <param name="configure">Registers the view's own plugins.</param>
    public ComposerEditorBuilder AddViewPlugin<TPlugin>(Action<ComposerViewBuilder> configure)
        where TPlugin : class, IComposerViewPlugin
    {
        ArgumentNullException.ThrowIfNull(configure);

        var view = _scope.Add(typeof(TPlugin));
        var builder = new ComposerViewBuilder(view);

        configure(builder);
        _scope.SetChildren(view, builder.Collect);

        return this;
    }

    internal IEnumerable<ComposerPluginRegistration> Collect()
        => _scope.Resolve();
}
