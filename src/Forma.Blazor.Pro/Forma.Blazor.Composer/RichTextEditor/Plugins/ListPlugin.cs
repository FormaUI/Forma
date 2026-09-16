namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

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

    private readonly RichTextEditorPlugin _editorPlugin = editor;

    /// <inheritdoc />
    public string Id => "list";

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
                    new FormaToolbarToggle { Id = "bullet-list", Title = "Bulleted List", Icon = Glyph.ListBullet, Command = BulletListCommand },
                    new FormaToolbarToggle { Id = "ordered-list", Title = "Numbered List", Icon = Glyph.ListNumbered, Command = OrderedListCommand },
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
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

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
            _editorPlugin.RequestCommand(jsCommand);
        }

        return ValueTask.CompletedTask;
    }
}
