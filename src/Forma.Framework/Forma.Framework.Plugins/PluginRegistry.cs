namespace Forma.Framework.Plugins;

/// <summary>What one owner registers: its plugins in order, the defaults it falls back on, and what each of them registers in turn.</summary>
/// <typeparam name="TPlugin">The plugin family this registers.</typeparam>
/// <remarks>
/// A registry records what to create rather than what was created, so one description serves every owner that
/// activates it and no two of them share a plugin.
/// </remarks>
public sealed class PluginRegistry<TPlugin>
    where TPlugin : class, IPlugin
{
    private sealed record Entry(PluginRegistration<TPlugin> Registration)
    {
        public PluginRegistry<TPlugin>? Children { get; set; }
    }

    private readonly List<Entry> _registered = [];
    private readonly List<PluginRegistration<TPlugin>> _defaults = [];
    private readonly PluginRegistration<TPlugin>? _owner;

    /// <summary>Creates a registry for plugins registered under nothing.</summary>
    public PluginRegistry()
    {
    }

    private PluginRegistry(PluginRegistration<TPlugin> owner)
    {
        _owner = owner;
    }

    /// <summary>Registers a plugin, and whatever is registered under it.</summary>
    /// <typeparam name="TImplementation">The plugin to create.</typeparam>
    /// <param name="children">Registers the plugins that sit under this one.</param>
    public PluginRegistry<TPlugin> Add<TImplementation>(Action<PluginRegistry<TPlugin>>? children = null)
        where TImplementation : class, TPlugin
        => Record(typeof(TImplementation), optionsType: null, configure: null, factory: null, children);

    /// <summary>Registers a plugin, configuring the options it takes, and whatever is registered under it.</summary>
    /// <typeparam name="TImplementation">The plugin to create.</typeparam>
    /// <typeparam name="TOptions">The options record the plugin declares through <see cref="IPluginConfigurable{TOptions}"/>.</typeparam>
    /// <param name="configure">Fills the options record created for this registration.</param>
    /// <param name="children">Registers the plugins that sit under this one.</param>
    public PluginRegistry<TPlugin> Add<TImplementation, TOptions>(
        Action<TOptions> configure,
        Action<PluginRegistry<TPlugin>>? children = null)
        where TImplementation : class, TPlugin, IPluginConfigurable<TOptions>
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(configure);

        return Record(typeof(TImplementation), typeof(TOptions), options => configure((TOptions)options), factory: null, children);
    }

    /// <summary>Registers a plugin the given factory creates rather than one built from its constructor.</summary>
    /// <typeparam name="TImplementation">The plugin the factory creates.</typeparam>
    /// <param name="factory">Creates the plugin for one owner, from the host's services.</param>
    /// <param name="children">Registers the plugins that sit under this one.</param>
    /// <remarks>What a factory creates is left undisposed, because a factory answering one instance to every owner is the host's to govern.</remarks>
    public PluginRegistry<TPlugin> Add<TImplementation>(
        Func<IServiceProvider, TImplementation> factory,
        Action<PluginRegistry<TPlugin>>? children = null)
        where TImplementation : class, TPlugin
    {
        ArgumentNullException.ThrowIfNull(factory);

        return Record(typeof(TImplementation), optionsType: null, configure: null, services => factory(services), children);
    }

    /// <summary>Registers a plugin unless this owner registers one of its own.</summary>
    /// <typeparam name="TImplementation">The plugin to fall back on.</typeparam>
    public PluginRegistry<TPlugin> TryAdd<TImplementation>()
        where TImplementation : class, TPlugin
    {
        if (!_defaults.Exists(held => held.PluginType == typeof(TImplementation)))
        {
            _defaults.Add(Create(typeof(TImplementation), optionsType: null, configure: null, factory: null));
        }

        return this;
    }

    /// <summary>Builds this registry's plugins for one owner, which disposes the scope when it is done with them.</summary>
    /// <param name="services">The host's services, which stand in for whatever a constructor asks for and no registered plugin supplies.</param>
    /// <exception cref="InvalidOperationException">A plugin takes something nothing can supply, or takes a plugin that takes it back.</exception>
    public PluginScope<TPlugin> Activate(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return PluginActivator.Activate([.. Resolve()], services);
    }

    /// <summary>This registry's registrations, each followed by whatever sits under it, and its uncovered defaults last.</summary>
    internal IEnumerable<PluginRegistration<TPlugin>> Resolve()
    {
        foreach (var entry in _registered)
        {
            yield return entry.Registration;

            foreach (var child in entry.Children?.Resolve() ?? [])
            {
                yield return child;
            }
        }

        foreach (var fallback in _defaults.Where(fallback => !_registered.Exists(held => held.Registration.PluginType == fallback.PluginType)))
        {
            yield return fallback;
        }
    }

    private PluginRegistry<TPlugin> Record(
        Type pluginType,
        Type? optionsType,
        Action<object>? configure,
        Func<IServiceProvider, TPlugin>? factory,
        Action<PluginRegistry<TPlugin>>? children)
    {
        var entry = new Entry(Create(pluginType, optionsType, configure, factory));

        if (children is not null)
        {
            var nested = new PluginRegistry<TPlugin>(entry.Registration);

            children(nested);
            entry.Children = nested;
        }

        var standing = _registered.FindIndex(held => held.Registration.PluginType == pluginType);

        // Replaced where it stood rather than moved to the end, so re-registering one plugin leaves the rest in place.
        if (standing >= 0)
        {
            _registered[standing] = entry;
        }
        else
        {
            _registered.Add(entry);
        }

        return this;
    }

    private PluginRegistration<TPlugin> Create(
        Type pluginType,
        Type? optionsType,
        Action<object>? configure,
        Func<IServiceProvider, TPlugin>? factory)
        => new()
        {
            PluginType = pluginType,
            OptionsType = optionsType,
            Configure = configure,
            Factory = factory,
            Owner = _owner,
        };
}
