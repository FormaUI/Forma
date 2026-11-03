namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Plugins;
using Forma.Framework.Toolbar;

/// <summary>Contributes basic text formatting operations: bold, italic, underline, strikethrough, inline code, subscript, superscript, and clear formatting.</summary>
public sealed class BasicFormattingPlugin(
    RichTextEditorPlugin editor,
    BasicFormattingPluginOptions? options = null) : IComposerFeaturePlugin, IPluginConfigurable<BasicFormattingPluginOptions>
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

    /// <summary>The options configuring the formatting offered.</summary>
    public BasicFormattingPluginOptions Options { get; } = options ?? BasicFormattingPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "basic-formatting";

    private bool IncludesScripts
        => Options.IncludeSubscriptAndSuperscript;

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new ToolbarGroup
            {
                Id = "inline-style",
                Title = "Inline Style",
                Items =
                [
                    new ToolbarToggle { Id = "bold", Title = "Bold", Icon = Glyph.Bold, Command = BoldCommand },
                    new ToolbarToggle { Id = "italic", Title = "Italic", Icon = Glyph.Italic, Command = ItalicCommand },
                    new ToolbarToggle { Id = "underline", Title = "Underline", Icon = Glyph.Underline, Command = UnderlineCommand },
                    new ToolbarToggle { Id = "strike", Title = "Strikethrough", Icon = Glyph.Strikethrough, Command = StrikeCommand },
                ],
            },
            new ToolbarGroup
            {
                Id = "text-effects",
                Title = "Text Effects",
                Items =
                [
                    new ToolbarToggle { Id = "code", Title = "Code", Icon = Glyph.Code, Command = CodeCommand },
                    .. (IncludesScripts
                        ? (ToolbarItem[])
                        [
                            new ToolbarToggle { Id = "subscript", Title = "Subscript", Icon = Glyph.Subscript, Command = SubscriptCommand },
                            new ToolbarToggle { Id = "superscript", Title = "Superscript", Icon = Glyph.Superscript, Command = SuperscriptCommand },
                        ]
                        : []),
                    new ToolbarCommand { Id = "remove-format", Title = "Clear Formatting", Icon = Glyph.ClearFormatting, Command = RemoveFormatCommand },
                ],
            },
        ]),
        new(ComposerRows.Selection,
        [
            new ToolbarGroup
            {
                Id = "selection-inline",
                Title = "Inline Style",
                Items =
                [
                    new ToolbarToggle { Id = "bold", Title = "Bold", Icon = Glyph.Bold, Command = BoldCommand },
                    new ToolbarToggle { Id = "italic", Title = "Italic", Icon = Glyph.Italic, Command = ItalicCommand },
                    new ToolbarToggle { Id = "underline", Title = "Underline", Icon = Glyph.Underline, Command = UnderlineCommand },
                    new ToolbarToggle { Id = "code", Title = "Code", Icon = Glyph.Code, Command = CodeCommand },
                ],
            },
        ]),
    ];

    // Code has no shortcut, since Ctrl+E is alignment.
    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "b", Command = BoldCommand, Control = true },
        new() { Key = "i", Command = ItalicCommand, Control = true },
        new() { Key = "u", Command = UnderlineCommand, Control = true },
        new() { Key = "x", Command = StrikeCommand, Control = true, Shift = true },
        .. (IncludesScripts
            ? (ComposerKeyBinding[])
            [
                new() { Key = ",", Command = SubscriptCommand, Control = true },
                new() { Key = ".", Command = SuperscriptCommand, Control = true },
            ]
            : []),
        new() { Key = "\\", Command = RemoveFormatCommand, Control = true },
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
            SubscriptCommand when IncludesScripts => "subscript",
            SuperscriptCommand when IncludesScripts => "superscript",
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
