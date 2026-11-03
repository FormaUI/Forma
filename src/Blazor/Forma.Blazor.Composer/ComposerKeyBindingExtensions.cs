namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Plugins;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;

/// <summary>Checks a composer's shortcut against the host's input policy.</summary>
internal static class ComposerKeyBindingExtensions
{
    /// <summary>Whether one of the host's preemptive routes claims the binding's keys.</summary>
    /// <remarks>
    /// A claimed press never reaches the composer. The host's key binding is read as the router matches it: Control and
    /// Alt count only where stated, a Shift left unstated matches either way, and a release binding or one requiring Meta
    /// claims nothing.
    /// </remarks>
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
