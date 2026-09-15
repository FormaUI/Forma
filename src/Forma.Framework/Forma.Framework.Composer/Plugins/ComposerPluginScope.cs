namespace Forma.Framework.Composer.Plugins;

/// <summary>Collects the plugins registered at one place in a composer's tree, and the defaults to fall back on.</summary>
/// <remarks>
/// Defaults are held back until the definition is built, so a scope's own registrations win over them
/// however early the defaults were asked for.
/// </remarks>
internal sealed class ComposerPluginScope(ComposerPluginRegistration? owner)
{
    private sealed record Entry(ComposerPluginRegistration Registration)
    {
        public Func<IEnumerable<ComposerPluginRegistration>>? Children { get; set; }
    }

    private readonly List<Entry> _registered = [];
    private readonly List<ComposerPluginRegistration> _defaults = [];

    /// <summary>The plugin this scope's registrations sit under, or null at the composer's root.</summary>
    public ComposerPluginRegistration? Owner { get; } = owner;

    /// <summary>Records a plugin this scope asks for by name, replacing an earlier registration of it where it stood.</summary>
    public ComposerPluginRegistration Add(
        Type pluginType,
        Type? optionsType = null,
        Action<object>? configure = null,
        Func<IServiceProvider, IComposerPlugin>? factory = null)
    {
        var entry = new Entry(Create(pluginType, optionsType, configure, factory));
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

        return entry.Registration;
    }

    /// <summary>Gives a registration the plugins registered under it, which are collected when the definition is built.</summary>
    public void SetChildren(ComposerPluginRegistration registration, Func<IEnumerable<ComposerPluginRegistration>> children)
        => _registered.Single(held => held.Registration == registration).Children = children;

    /// <summary>Records a plugin to add only where this scope registers none of its own.</summary>
    public void TryAdd(Type pluginType)
    {
        if (!_defaults.Exists(held => held.PluginType == pluginType))
        {
            _defaults.Add(Create(pluginType, optionsType: null, configure: null, factory: null));
        }
    }

    /// <summary>This scope's registrations, each followed by whatever was registered under it, and its uncovered defaults last.</summary>
    public IEnumerable<ComposerPluginRegistration> Resolve()
    {
        foreach (var entry in _registered)
        {
            yield return entry.Registration;

            foreach (var child in entry.Children?.Invoke() ?? [])
            {
                yield return child;
            }
        }

        foreach (var fallback in _defaults.Where(fallback => !_registered.Exists(held => held.Registration.PluginType == fallback.PluginType)))
        {
            yield return fallback;
        }
    }

    private ComposerPluginRegistration Create(
        Type pluginType,
        Type? optionsType,
        Action<object>? configure,
        Func<IServiceProvider, IComposerPlugin>? factory)
        => new()
        {
            PluginType = pluginType,
            OptionsType = optionsType,
            Configure = configure,
            Factory = factory,
            Owner = Owner,
        };
}
