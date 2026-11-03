namespace Forma.Framework.Composer.Plugins;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Plugins;

/// <summary>Describes the plugins a composer has: its editors, the views over them, and the capabilities around both.</summary>
/// <remarks>The description is built once, and each composer activates its own plugins from it.</remarks>
public sealed class ComposerPluginBuilder
{
    private readonly PluginRegistry<IComposerPlugin> _registry = new();

    internal ComposerPluginBuilder()
    {
    }

    /// <summary>Registers an editor, and the plugins and views that belong to it.</summary>
    /// <typeparam name="TPlugin">The editor to create.</typeparam>
    /// <param name="configure">Registers the editor's own plugins and views.</param>
    public ComposerPluginBuilder AddEditorPlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin>(Action<ComposerEditorBuilder<TPlugin>>? configure = null)
        where TPlugin : class, IComposerEditorPlugin
    {
        _registry.Add<TPlugin>(editor => configure?.Invoke(new ComposerEditorBuilder<TPlugin>(editor)));

        return this;
    }

    /// <summary>Registers an editor created through the given factory, and the plugins and views that belong to it.</summary>
    /// <typeparam name="TPlugin">The editor the factory creates.</typeparam>
    /// <param name="factory">Creates the editor for one composer, from the host's services.</param>
    /// <param name="configure">Registers the editor's own plugins and views.</param>
    /// <remarks>The factory runs once for each composer, and each composer uses the instance it returns.</remarks>
    public ComposerPluginBuilder AddEditorPlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin>(
        Func<IServiceProvider, TPlugin> factory,
        Action<ComposerEditorBuilder<TPlugin>>? configure = null)
        where TPlugin : class, IComposerEditorPlugin
    {
        _registry.Add(factory, editor => configure?.Invoke(new ComposerEditorBuilder<TPlugin>(editor)));

        return this;
    }

    /// <summary>Registers a capability that spans every editor and view.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    public ComposerPluginBuilder AddFeaturePlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _registry.Add<TPlugin>();

        return this;
    }

    /// <summary>Registers a capability that spans every editor and view, configuring the options it takes.</summary>
    /// <typeparam name="TPlugin">The plugin to create.</typeparam>
    /// <typeparam name="TOptions">The options record the plugin declares through <see cref="IPluginConfigurable{TOptions}"/>.</typeparam>
    /// <param name="configure">Fills the options record created for this registration.</param>
    public ComposerPluginBuilder AddFeaturePlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin, TOptions>(Action<TOptions> configure)
        where TPlugin : class, IComposerFeaturePlugin, IPluginConfigurable<TOptions>
        where TOptions : class, new()
    {
        _registry.Add<TPlugin, TOptions>(configure);

        return this;
    }

    /// <summary>Registers a capability that spans every editor and view, created through the given factory.</summary>
    /// <typeparam name="TPlugin">The plugin the factory creates.</typeparam>
    /// <param name="factory">Creates the plugin for one composer, from the host's services.</param>
    /// <remarks>The factory runs once for each composer, and each composer uses the instance it returns.</remarks>
    public ComposerPluginBuilder AddFeaturePlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin>(Func<IServiceProvider, TPlugin> factory)
        where TPlugin : class, IComposerFeaturePlugin
    {
        _registry.Add(factory);

        return this;
    }

    /// <summary>Registers a capability at the composer's root unless the root already registers it itself.</summary>
    /// <typeparam name="TPlugin">The plugin to fall back on.</typeparam>
    public ComposerPluginBuilder TryAddFeaturePlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin>()
        where TPlugin : class, IComposerFeaturePlugin
    {
        _registry.TryAdd<TPlugin>();

        return this;
    }

    /// <summary>Builds the definition of what has been registered.</summary>
    internal ComposerDefinition Build()
        => new(_registry);
}
