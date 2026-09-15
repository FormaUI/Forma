namespace Forma.Framework.Plugins;

/// <summary>One constructed plugin, what it was registered under, and who governs how long it lives.</summary>
/// <typeparam name="TPlugin">The plugin family this belongs to.</typeparam>
public sealed class PluginInstance<TPlugin>
    where TPlugin : class, IPlugin
{
    /// <summary>The plugin itself.</summary>
    public required TPlugin Plugin { get; init; }

    /// <summary>What this plugin was registered under, or null at the root.</summary>
    public PluginInstance<TPlugin>? Owner { get; init; }

    /// <summary>Whether the plugin's lifetime is managed independently of the scope, so the scope leaves it undisposed.</summary>
    public bool IsAutonomous { get; init; }
}
