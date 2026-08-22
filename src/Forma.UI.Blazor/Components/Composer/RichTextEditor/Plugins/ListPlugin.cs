namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Style.Glyphs;

/// <summary>Contributes bulleted and numbered lists, indentation, and outdent operations.</summary>
public sealed class ListPlugin(RichTextEditorPlugin editor) : IComposerPlugin
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
    public string DefaultRow => ComposerRows.Paragraph;

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> ToolbarItems =>
    [
        new() { Id = "bullet-list", Title = "Bulleted List", Icon = Glyph.ListUl, Command = BulletListCommand, Group = "list", IsToggle = true },
        new() { Id = "ordered-list", Title = "Numbered List", Icon = Glyph.ListOl, Command = OrderedListCommand, Group = "list", IsToggle = true },
        new() { Id = "indent", Title = "Indent", Icon = Glyph.Indent, Command = IndentCommand, Group = "indent" },
        new() { Id = "outdent", Title = "Outdent", Icon = Glyph.Outdent, Command = OutdentCommand, Group = "indent" },
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];

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
