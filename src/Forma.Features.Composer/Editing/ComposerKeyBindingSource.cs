namespace Forma.Features.Composer.Editing;

using Forma.Features.Composer.Plugins;

/// <summary>The key bindings the composer engine ships for the commands it handles itself.</summary>
public sealed class ComposerKeyBindingSource : IComposerKeyBindingSource
{
    /// <summary>The shared source of the engine's own bindings.</summary>
    public static ComposerKeyBindingSource Default { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings { get; } =
    [
        new() { Key = "z", Command = ComposerCommands.Undo, Control = true },
        new() { Key = "y", Command = ComposerCommands.Redo, Control = true },
        new() { Key = "z", Command = ComposerCommands.Redo, Control = true, Shift = true },
    ];
}
