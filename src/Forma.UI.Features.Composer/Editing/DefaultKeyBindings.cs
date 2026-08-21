namespace Forma.UI.Features.Composer.Editing;

using Forma.UI.Features.Composer.Plugins;

/// <summary>The key bindings the composer engine ships for the commands it handles itself.</summary>
public sealed class DefaultKeyBindings : IComposerKeyBindingProvider
{
    /// <summary>The shared instance of the engine's bindings.</summary>
    public static DefaultKeyBindings Instance { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings { get; } =
    [
        new() { Key = "z", Command = ComposerCommands.Undo, Control = true },
        new() { Key = "y", Command = ComposerCommands.Redo, Control = true },
        new() { Key = "z", Command = ComposerCommands.Redo, Control = true, Shift = true },
    ];
}
