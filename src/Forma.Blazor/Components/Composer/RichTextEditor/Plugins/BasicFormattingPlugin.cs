namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Features.Composer.Assets;
using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes basic text formatting operations: bold, italic, underline, strikethrough, inline code, subscript, superscript, and clear formatting.</summary>
public sealed class BasicFormattingPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command applying bold styling.</summary>
    public const string BoldCommand = "format.bold";

    /// <summary>The command applying italic styling.</summary>
    public const string ItalicCommand = "format.italic";

    /// <summary>The command applying underline styling.</summary>
    public const string UnderlineCommand = "format.underline";

    /// <summary>The command applying strikethrough styling.</summary>
    public const string StrikeCommand = "format.strike";

    /// <summary>The command wrapping in inline code.</summary>
    public const string CodeCommand = "format.code";

    /// <summary>The command applying subscript styling.</summary>
    public const string SubscriptCommand = "format.subscript";

    /// <summary>The command applying superscript styling.</summary>
    public const string SuperscriptCommand = "format.superscript";

    /// <summary>The command removing formatting from the selection.</summary>
    public const string RemoveFormatCommand = "format.remove-format";

    private readonly RichTextEditorPlugin _editorPlugin = editor;

    /// <inheritdoc />
    public string Id => "basic-formatting";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new FormaToolbarGroup
            {
                Id = "inline-style",
                Title = "Inline Style",
                Items =
                [
                    new FormaToolbarToggle { Id = "bold", Title = "Bold", Icon = Glyph.Bold, Command = BoldCommand },
                    new FormaToolbarToggle { Id = "italic", Title = "Italic", Icon = Glyph.Italic, Command = ItalicCommand },
                    new FormaToolbarToggle { Id = "underline", Title = "Underline", Icon = Glyph.Underline, Command = UnderlineCommand },
                    new FormaToolbarToggle { Id = "strike", Title = "Strikethrough", Icon = Glyph.Strikethrough, Command = StrikeCommand },
                ],
            },
            new FormaToolbarGroup
            {
                Id = "text-effects",
                Title = "Text Effects",
                Items =
                [
                    new FormaToolbarToggle { Id = "code", Title = "Code", Icon = Glyph.Code, Command = CodeCommand },
                    new FormaToolbarToggle { Id = "subscript", Title = "Subscript", Icon = Glyph.Subscript, Command = SubscriptCommand },
                    new FormaToolbarToggle { Id = "superscript", Title = "Superscript", Icon = Glyph.Superscript, Command = SuperscriptCommand },
                    new FormaToolbarCommand { Id = "remove-format", Title = "Clear Formatting", Icon = Glyph.ClearFormatting, Command = RemoveFormatCommand },
                ],
            },
        ]),
        new(ComposerRows.Selection,
        [
            new FormaToolbarGroup
            {
                Id = "selection-inline",
                Title = "Inline Style",
                Items =
                [
                    new FormaToolbarToggle { Id = "bold", Title = "Bold", Icon = Glyph.Bold, Command = BoldCommand },
                    new FormaToolbarToggle { Id = "italic", Title = "Italic", Icon = Glyph.Italic, Command = ItalicCommand },
                    new FormaToolbarToggle { Id = "underline", Title = "Underline", Icon = Glyph.Underline, Command = UnderlineCommand },
                    new FormaToolbarToggle { Id = "code", Title = "Code", Icon = Glyph.Code, Command = CodeCommand },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "b", Command = BoldCommand, Control = true },
        new() { Key = "i", Command = ItalicCommand, Control = true },
        new() { Key = "u", Command = UnderlineCommand, Control = true },
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

        var jsCommand = command switch
        {
            BoldCommand => "bold",
            ItalicCommand => "italic",
            UnderlineCommand => "underline",
            StrikeCommand => "strikeThrough",
            CodeCommand => "code",
            SubscriptCommand => "subscript",
            SuperscriptCommand => "superscript",
            RemoveFormatCommand => "removeFormat",
            _ => null,
        };

        if (jsCommand is not null)
        {
            _editorPlugin.RequestCommand(jsCommand);
        }

        return ValueTask.CompletedTask;
    }
}
