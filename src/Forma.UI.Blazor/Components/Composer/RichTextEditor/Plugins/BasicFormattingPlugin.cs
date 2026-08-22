namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Style.Glyphs;

/// <summary>Contributes basic text formatting operations: bold, italic, underline, strikethrough, inline code, subscript, superscript, and clear formatting.</summary>
public sealed class BasicFormattingPlugin(RichTextEditorPlugin editor) : IComposerPlugin
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

    private readonly RichTextEditorPlugin _editor = editor;

    /// <inheritdoc />
    public string Id => "basic-formatting";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public string DefaultRow => ComposerRows.Formatting;

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> ToolbarItems =>
    [
        new() { Id = "bold", Title = "Bold", Icon = Glyph.Bold, Command = BoldCommand, Group = "inline", IsToggle = true },
        new() { Id = "italic", Title = "Italic", Icon = Glyph.Italic, Command = ItalicCommand, Group = "inline", IsToggle = true },
        new() { Id = "underline", Title = "Underline", Icon = Glyph.Underline, Command = UnderlineCommand, Group = "inline", IsToggle = true },
        new() { Id = "strike", Title = "Strikethrough", Icon = Glyph.Strikethrough, Command = StrikeCommand, Group = "inline", IsToggle = true },
        new() { Id = "code", Title = "Code", Icon = Glyph.Code, Command = CodeCommand, Group = "script", IsToggle = true },
        new() { Id = "subscript", Title = "Subscript", Icon = Glyph.Subscript, Command = SubscriptCommand, Group = "script", IsToggle = true },
        new() { Id = "superscript", Title = "Superscript", Icon = Glyph.Superscript, Command = SuperscriptCommand, Group = "script", IsToggle = true },
        new() { Id = "remove-format", Title = "Clear Formatting", Icon = Glyph.TextSlash, Command = RemoveFormatCommand, Group = "script" },
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "b", Command = BoldCommand, Control = true },
        new() { Key = "i", Command = ItalicCommand, Control = true },
        new() { Key = "u", Command = UnderlineCommand, Control = true },
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
            _editor.RequestCommand(jsCommand);
        }

        return ValueTask.CompletedTask;
    }
}
