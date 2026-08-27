namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>Contributes bulleted and numbered lists, indentation, and outdent operations.</summary>
public sealed class ListPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command inserting or toggling a bulleted list.</summary>
    public const string BulletListCommand = "list.bullet";

    /// <summary>The command inserting or toggling a numbered list.</summary>
    public const string OrderedListCommand = "list.ordered";

    /// <summary>The command increasing indentation.</summary>
    public const string IndentCommand = "list.indent";

    /// <summary>The command decreasing indentation.</summary>
    public const string OutdentCommand = "list.outdent";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <inheritdoc />
    public string Id => "list";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Paragraph,
        [
            new FormaToolbarGroup
            {
                Id = "lists",
                Title = "Lists",
                Items =
                [
                    new FormaToolbarToggle { Id = "bullet-list", Title = "Bulleted List", Icon = Glyph.BulletList, Command = BulletListCommand },
                    new FormaToolbarToggle { Id = "ordered-list", Title = "Numbered List", Icon = Glyph.NumberedList, Command = OrderedListCommand },
                ],
            },
            new FormaToolbarGroup
            {
                Id = "indentation",
                Title = "Indentation",
                Items =
                [
                    new FormaToolbarCommand { Id = "indent", Title = "Indent", Icon = Glyph.Indent, Command = IndentCommand },
                    new FormaToolbarCommand { Id = "outdent", Title = "Outdent", Icon = Glyph.Outdent, Command = OutdentCommand },
                ],
            },
        ]),
    ];

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
            BulletListCommand => "insertUnorderedList",
            OrderedListCommand => "insertOrderedList",
            IndentCommand => "indent",
            OutdentCommand => "outdent",
            _ => null,
        };

        if (jsCommand is not null)
        {
            _editor.RequestCommand(jsCommand);
        }

        return ValueTask.CompletedTask;
    }
}
