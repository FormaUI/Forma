namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>The feature plugin that opens the reference of the shortcuts in play.</summary>
public sealed class ShortcutsPlugin : IComposerFeaturePlugin
{
    /// <inheritdoc />
    public string Id => "shortcuts";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.BuiltIn,
        [
            new ToolbarGroup
            {
                Id = "view",
                Title = "View",
                Items =
                [
                    new ToolbarCommand
                    {
                        Id = "shortcuts",
                        Title = "Keyboard Shortcuts",
                        Icon = Glyph.Keyboard,
                        Command = ComposerCommands.Shortcuts,
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "/", Command = ComposerCommands.Shortcuts, Control = true },
    ];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);
}
