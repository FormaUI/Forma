namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Glyphs;

/// <summary>Contributes clipboard and selection commands to the rich text editor.</summary>
public sealed class ClipboardPlugin(RichTextEditorPlugin editor) : IComposerPlugin
{
    /// <summary>The command selecting the entire document content.</summary>
    public const string SelectAllCommand = "clipboard.select-all";

    /// <summary>The command pasting clipboard contents as plain text.</summary>
    public const string PasteTextCommand = "clipboard.paste-text";

    /// <summary>The command pasting clipboard contents sanitized from Microsoft Word formatting.</summary>
    public const string PasteWordCommand = "clipboard.paste-word";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <inheritdoc />
    public string Id => "clipboard";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new FormaToolbarGroup
            {
                Id = "clipboard",
                Title = "Clipboard",
                Items =
                [
                    new FormaToolbarCommand
                    {
                        Id = "select-all",
                        Title = "Select All",
                        Icon = Glyph.SquareCheck,
                        Command = SelectAllCommand,
                        ShortcutDisplay = "Ctrl+A",
                        ShortcutAriaKeys = "Control+a",
                    },
                    new FormaToolbarCommand
                    {
                        Id = "paste-text",
                        Title = "Paste as Plain Text",
                        Icon = Glyph.Paste,
                        Command = PasteTextCommand,
                    },
                    new FormaToolbarCommand
                    {
                        Id = "paste-word",
                        Title = "Paste from Word",
                        Icon = Glyph.FileLines,
                        Command = PasteWordCommand,
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "a", Command = SelectAllCommand, Control = true },
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets => [];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);

        switch (command)
        {
            case SelectAllCommand:
                _editor.RequestCommand("selectAll");
                break;

            case PasteTextCommand:
                _editor.RequestCommand("pasteText");
                break;

            case PasteWordCommand:
                _editor.RequestCommand("pasteWord");
                break;
        }

        return ValueTask.CompletedTask;
    }
}
