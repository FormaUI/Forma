namespace Forma.Framework.Composer.Plugins;

/// <summary>One plugin a definition will build: what to create, how to configure it, and the plugin it was registered under.</summary>
internal sealed class ComposerPluginRegistration
{
    /// <summary>The plugin to create.</summary>
    public required Type PluginType { get; init; }

    /// <summary>The options record the plugin takes, or null when it takes none.</summary>
    public Type? OptionsType { get; init; }

    /// <summary>Fills a freshly created options record, or null when the registration configured none.</summary>
    public Action<object>? Configure { get; init; }

    /// <summary>Creates the plugin itself, or null to build it from its constructor.</summary>
    public Func<IServiceProvider, IComposerPlugin>? Factory { get; init; }

    /// <summary>The registration this one sits under, or null at the composer's root.</summary>
    public ComposerPluginRegistration? Owner { get; init; }
}
