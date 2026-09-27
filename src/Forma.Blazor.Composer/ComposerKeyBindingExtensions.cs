namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Plugins;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;

/// <summary>What a host's input policy makes of a composer's shortcut.</summary>
internal static class ComposerKeyBindingExtensions
{
    /// <summary>
    /// Whether one of the host's preemptive routes answers the binding's keys, in which case the press never reaches
    /// the composer. The host's key binding is read as the router matches it: Control and Alt count only where stated,
    /// and Shift left unstated matches either way.
    /// </summary>
    public static bool IsClaimedBy(this ComposerKeyBinding binding, InputRouterOptions options)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(options);

        return options.PreemptiveRoutes
            .SelectMany(route => route.Bindings)
            .OfType<KeyBinding>()
            .Any(host => Claims(host, binding));
    }

    private static bool Claims(KeyBinding host, ComposerKeyBinding binding)
        => !host.Released
            && string.Equals(host.Key, binding.Key, StringComparison.OrdinalIgnoreCase)
            && host.Modifiers.Meta is not true
            && (host.Modifiers.Primary || host.Modifiers.Control is true) == binding.Control
            && (host.Modifiers.Alt ?? false) == binding.Alt
            && (host.Modifiers.Shift ?? binding.Shift) == binding.Shift;
}
