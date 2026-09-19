namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes paragraph and block-level formatting operations: paragraph, headings 1–6, blockquote, and lead text.</summary>
public sealed class ParagraphFormatPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command applying normal paragraph block styling.</summary>
    public const string ParagraphCommand = "paragraph.p";

    /// <summary>The command applying heading 1 block styling.</summary>
    public const string Heading1Command = "paragraph.h1";

    /// <summary>The command applying heading 2 block styling.</summary>
    public const string Heading2Command = "paragraph.h2";

    /// <summary>The command applying heading 3 block styling.</summary>
    public const string Heading3Command = "paragraph.h3";

    /// <summary>The command applying heading 4 block styling.</summary>
    public const string Heading4Command = "paragraph.h4";

    /// <summary>The command applying heading 5 block styling.</summary>
    public const string Heading5Command = "paragraph.h5";

    /// <summary>The command applying heading 6 block styling.</summary>
    public const string Heading6Command = "paragraph.h6";

    /// <summary>The command applying blockquote block styling.</summary>
    public const string BlockquoteCommand = "paragraph.blockquote";

    /// <summary>The command toggling lead paragraph styling.</summary>
    public const string LeadCommand = "paragraph.lead";

    /// <summary>The block styles on offer, from a paragraph through the six headings to a quote.</summary>
    private static readonly ToolbarMenu BlockStyleMenu = new()
    {
        Id = "block-style",
        Title = "Block Style",
        Icon = Glyph.Heading,
        Options =
        [
            new() { Id = "p", Title = "Normal", Command = ParagraphCommand },
            new() { Id = "h1", Title = "Heading 1", Command = Heading1Command, PreviewStyle = "font-size:1.75rem;font-weight:700" },
            new() { Id = "h2", Title = "Heading 2", Command = Heading2Command, PreviewStyle = "font-size:1.5rem;font-weight:700" },
            new() { Id = "h3", Title = "Heading 3", Command = Heading3Command, PreviewStyle = "font-size:1.25rem;font-weight:600" },
            new() { Id = "h4", Title = "Heading 4", Command = Heading4Command, PreviewStyle = "font-size:1.125rem;font-weight:600" },
            new() { Id = "h5", Title = "Heading 5", Command = Heading5Command, PreviewStyle = "font-size:1rem;font-weight:600" },
            new() { Id = "h6", Title = "Heading 6", Command = Heading6Command, PreviewStyle = "font-size:0.875rem;font-weight:600" },
            new() { Id = "blockquote", Title = "Quote", Command = BlockquoteCommand, PreviewStyle = "font-style:italic" },
        ],
    };

    private readonly RichTextEditorPlugin _editorPlugin = editor;

    /// <inheritdoc />
    public string Id => "paragraph-format";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new ToolbarGroup
            {
                Id = "paragraph-style",
                Title = "Paragraph Style",
                Items = [BlockStyleMenu],
            },
        ]),
        new(ComposerRows.Paragraph,
        [
            new ToolbarGroup
            {
                Id = "paragraph-style",
                Title = "Paragraph Style",
                Items = [new ToolbarToggle { Id = "lead", Title = "Lead", Icon = Glyph.Paragraph, Command = LeadCommand }],
            },
        ]),
        new(ComposerRows.Selection,
        [
            new ToolbarGroup
            {
                Id = "selection-paragraph",
                Title = "Paragraph Style",
                Items = [BlockStyleMenu],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "0", Command = ParagraphCommand, Control = true, Alt = true },
        new() { Key = "1", Command = Heading1Command, Control = true, Alt = true },
        new() { Key = "2", Command = Heading2Command, Control = true, Alt = true },
        new() { Key = "3", Command = Heading3Command, Control = true, Alt = true },
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

        if (command == LeadCommand)
        {
            _editorPlugin.RequestCommand("lead");
            return ValueTask.CompletedTask;
        }

        var blockTag = command switch
        {
            ParagraphCommand => "p",
            Heading1Command => "h1",
            Heading2Command => "h2",
            Heading3Command => "h3",
            Heading4Command => "h4",
            Heading5Command => "h5",
            Heading6Command => "h6",
            BlockquoteCommand => "blockquote",
            _ => null,
        };

        if (blockTag is not null)
        {
            _editorPlugin.RequestCommand("formatBlock", blockTag);
        }

        return ValueTask.CompletedTask;
    }
}
