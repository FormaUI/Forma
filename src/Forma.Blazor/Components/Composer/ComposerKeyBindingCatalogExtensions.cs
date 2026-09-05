namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;

/// <summary>The input routes a composer's shortcut catalog registers with the input router.</summary>
internal static class ComposerKeyBindingCatalogExtensions
{
    /// <summary>One route per command, each bound to the command's keys; Control stands for the platform's shortcut key.</summary>
    public static IReadOnlyList<InputRoute> ToInputRoutes(this ComposerKeyBindingCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return [.. catalog.Bindings
            .GroupBy(binding => binding.Command, StringComparer.Ordinal)
            .Select(command => new InputRoute(command.Key, [.. command.Select(ToInputBinding)]))];
    }

    private static InputBinding ToInputBinding(ComposerKeyBinding binding)
        => new KeyBinding(binding.Key)
        {
            Modifiers = new ModifierRequirement { Primary = binding.Control, Alt = binding.Alt, Shift = binding.Shift },
        };
}
