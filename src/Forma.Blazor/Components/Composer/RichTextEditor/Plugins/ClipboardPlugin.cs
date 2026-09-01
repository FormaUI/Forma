namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Features.Composer.Assets;
using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes clipboard and selection commands to the rich text editor.</summary>
public sealed class ClipboardPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command selecting the entire document content.</summary>
    public const string SelectAllCommand = "clipboard.select-all";

    /// <summary>The command pasting clipboard contents as plain text.</summary>
    public const string PasteTextCommand = "clipboard.paste-text";

    /// <summary>The command pasting clipboard contents sanitized from Microsoft Word formatting.</summary>
    public const string PasteWordCommand = "clipboard.paste-word";

    private readonly RichTextEditorPlugin _editorPlugin = editor;

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
                        Icon = Glyph.SelectAll,
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
                        Icon = Glyph.FileText,
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
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        switch (command)
        {
            case SelectAllCommand:
                _editorPlugin.RequestCommand("selectAll");
                break;

            case PasteTextCommand:
                _editorPlugin.RequestCommand("pasteText");
                break;

            case PasteWordCommand:
                _editorPlugin.RequestCommand("pasteWord");
                break;
        }

        return ValueTask.CompletedTask;
    }
}
