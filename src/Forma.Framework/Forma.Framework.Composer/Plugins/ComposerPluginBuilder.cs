namespace Forma.Framework.Composer.Plugins;

/// <summary>Describes the plugins a composer has: its editors, the views over them, and the capabilities around both.</summary>
/// <remarks>
/// The description is built once and each composer activates its own plugins from it, because an editor plugin
/// carries events its canvas subscribes to and two composers sharing one would answer each other's commands.
/// </remarks>
public sealed class ComposerPluginBuilder
{
    private readonly ComposerPluginScope _scope = new(owner: null);

    /// <summary>Registers an editor.</summary>
    /// <typeparam name="TPlugin">The editor to create.</typeparam>
    public ComposerPluginBuilder AddEditorPlugin<TPlugin>()
        where TPlugin : class, IComposerEditorPlugin
        => AddEditorPlugin<TPlugin>(_ => { });

    /// <summary>Registers an editor, and the plugins and views that belong to it.</summary>
    /// <typeparam name="TPlugin">The editor to create.</typeparam>
    /// <param name="configure">Registers the editor's own plugins and views.</param>
    public ComposerPluginBuilder AddEditorPlugin<TPlugin>(Action<ComposerEditorBuilder> configure)
        where TPlugin : class, IComposerEditorPlugin
    {
        ArgumentNullException.ThrowIfNull(configure);

        var editor = _scope.Add(typeof(TPlugin));
        var builder = new ComposerEditorBuilder(editor);

        configure(builder);
        _scope.SetChildren(editor, builder.Collect);

        return this;
    }

    /// <summary>Registers a capability that spans every editor and view.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    public ComposerPluginBuilder AddFeaturePlugin<TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _scope.Add(typeof(TPlugin));

        return this;
    }

    /// <summary>Registers a capability that spans every editor and view, configuring the options it takes.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    /// <typeparam name="TOptions">The options record the plugin declares through <see cref="IComposerConfigurable{TOptions}"/>.</typeparam>
    /// <param name="configure">Fills the options record created for this registration.</param>
    public ComposerPluginBuilder AddFeaturePlugin<TPlugin, TOptions>(Action<TOptions> configure)
        where TPlugin : class, IComposerFeaturePlugin, IComposerConfigurable<TOptions>
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(configure);
        _scope.Add(typeof(TPlugin), typeof(TOptions), options => configure((TOptions)options));

        return this;
    }

    /// <summary>Registers a capability the composer creates through the given factory rather than from its constructor.</summary>
    /// <typeparam name="TPlugin">The plugin the factory creates.</typeparam>
    /// <param name="factory">Creates the plugin for one composer, from the host's services.</param>
    /// <remarks>A factory answering the same instance every time hands that instance to every composer, which an editor plugin must never do.</remarks>
    public ComposerPluginBuilder AddFeaturePlugin<TPlugin>(Func<IServiceProvider, TPlugin> factory)
        where TPlugin : class, IComposerFeaturePlugin
    {
        ArgumentNullException.ThrowIfNull(factory);
        _scope.Add(typeof(TPlugin), factory: services => factory(services));

        return this;
    }

    /// <summary>Registers an editor the composer creates through the given factory rather than from its constructor.</summary>
    /// <typeparam name="TPlugin">The editor the factory creates.</typeparam>
    /// <param name="factory">Creates the editor for one composer, from the host's services.</param>
    /// <param name="configure">Registers the editor's own plugins and views.</param>
    /// <remarks>A factory answering the same instance every time hands that instance to every composer, which an editor plugin must never do.</remarks>
    public ComposerPluginBuilder AddEditorPlugin<TPlugin>(
        Func<IServiceProvider, TPlugin> factory,
        Action<ComposerEditorBuilder>? configure = null)
        where TPlugin : class, IComposerEditorPlugin
    {
        ArgumentNullException.ThrowIfNull(factory);

        var editor = _scope.Add(typeof(TPlugin), factory: services => factory(services));
        var builder = new ComposerEditorBuilder(editor);

        (configure ?? (_ => { }))(builder);
        _scope.SetChildren(editor, builder.Collect);

        return this;
    }

    /// <summary>Registers a capability at the composer's root unless the root already registers it itself.</summary>
    /// <typeparam name="TPlugin">The plugin to fall back on.</typeparam>
    public ComposerPluginBuilder TryAddFeaturePlugin<TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _scope.TryAdd(typeof(TPlugin));

        return this;
    }

    /// <summary>Freezes what has been registered into a description every composer can activate.</summary>
    public ComposerDefinition Build()
        => new([.. _scope.Resolve()]);
}
