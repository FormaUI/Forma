namespace Forma.Framework.Plugins;

/// <summary>One plugin a registry will build: what to create, how to configure it, and what it sits under.</summary>
/// <typeparam name="TPlugin">The plugin family this belongs to.</typeparam>
internal sealed class PluginRegistration<TPlugin>
    where TPlugin : class, IPlugin
{
    /// <summary>The plugin to create.</summary>
    public required Type PluginType { get; init; }

    /// <summary>The options record the plugin takes.</summary>
    public Type? OptionsType { get; init; }

    /// <summary>Fills a freshly created options record.</summary>
    public Action<object>? Configure { get; init; }

    /// <summary>Creates the plugin itself, where null builds it from its constructor.</summary>
    public Func<IServiceProvider, TPlugin>? Factory { get; init; }

    /// <summary>The registration this one sits under, or null at the root.</summary>
    public PluginRegistration<TPlugin>? Owner { get; init; }
}
