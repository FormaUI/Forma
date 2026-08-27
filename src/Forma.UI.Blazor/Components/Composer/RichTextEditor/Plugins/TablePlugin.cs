namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>Contributes HTML table insertion and cell, row, and column manipulation commands to the rich text editor.</summary>
public sealed class TablePlugin(
    RichTextEditorPlugin editor,
    TablePluginOptions? options = null) : IComposerFeaturePlugin
{
    /// <summary>The command opening the insert table dialog.</summary>
    public const string InsertTableCommand = "table.insert";

    /// <summary>The command adding a new row above the active row.</summary>
    public const string AddRowBeforeCommand = "table.add-row-before";

    /// <summary>The command adding a new row below the active row.</summary>
    public const string AddRowAfterCommand = "table.add-row-after";

    /// <summary>The command deleting the active row.</summary>
    public const string DeleteRowCommand = "table.delete-row";

    /// <summary>The command adding a new column to the left of the active cell.</summary>
    public const string AddColBeforeCommand = "table.add-col-before";

    /// <summary>The command adding a new column to the right of the active cell.</summary>
    public const string AddColAfterCommand = "table.add-col-after";

    /// <summary>The command deleting the active column.</summary>
    public const string DeleteColCommand = "table.delete-col";

    /// <summary>The command toggling the header row of the active table.</summary>
    public const string ToggleHeaderCommand = "table.toggle-header";

    /// <summary>The command deleting the active table entirely.</summary>
    public const string DeleteTableCommand = "table.delete";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <summary>Options configuring table creation limits.</summary>
    public TablePluginOptions Options { get; } = options ?? TablePluginOptions.Default;

    /// <inheritdoc />
    public string Id => "table";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Paragraph,
        [
            new FormaToolbarGroup
            {
                Id = "table",
                Title = "Table",
                Items =
                [
                    new FormaToolbarMenu
                    {
                        Id = "table-menu",
                        Title = "Table",
                        Icon = Glyph.Table,
                        Options =
                        [
                            new FormaToolbarOption { Id = "table-insert", Title = "Insert Table...", Icon = Glyph.Table, Command = InsertTableCommand },
                            new FormaToolbarOption { Id = "table-row-before", Title = "Add Row Above", Icon = Glyph.AddRowAbove, Command = AddRowBeforeCommand },
                            new FormaToolbarOption { Id = "table-row-after", Title = "Add Row Below", Icon = Glyph.AddRowBelow, Command = AddRowAfterCommand },
                            new FormaToolbarOption { Id = "table-row-delete", Title = "Delete Row", Icon = Glyph.Rows, Command = DeleteRowCommand },
                            new FormaToolbarOption { Id = "table-col-before", Title = "Add Column Left", Icon = Glyph.AddColumnLeft, Command = AddColBeforeCommand },
                            new FormaToolbarOption { Id = "table-col-after", Title = "Add Column Right", Icon = Glyph.AddColumnRight, Command = AddColAfterCommand },
                            new FormaToolbarOption { Id = "table-col-delete", Title = "Delete Column", Icon = Glyph.Columns, Command = DeleteColCommand },
                            new FormaToolbarOption { Id = "table-toggle-header", Title = "Toggle Header Row", Icon = Glyph.Rows, Command = ToggleHeaderCommand },
                            new FormaToolbarOption { Id = "table-delete", Title = "Delete Table", Icon = Glyph.Delete, Command = DeleteTableCommand },
                        ],
                    },
                ],
            },
        ]),
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

        switch (command)
        {
            case InsertTableCommand:
                _editor.RequestTableDialog();
                break;

            case AddRowBeforeCommand:
                _editor.RequestCommand("tableAddRowBefore");
                break;

            case AddRowAfterCommand:
                _editor.RequestCommand("tableAddRowAfter");
                break;

            case DeleteRowCommand:
                _editor.RequestCommand("tableDeleteRow");
                break;

            case AddColBeforeCommand:
                _editor.RequestCommand("tableAddColBefore");
                break;

            case AddColAfterCommand:
                _editor.RequestCommand("tableAddColAfter");
                break;

            case DeleteColCommand:
                _editor.RequestCommand("tableDeleteCol");
                break;

            case ToggleHeaderCommand:
                _editor.RequestCommand("tableToggleHeader");
                break;

            case DeleteTableCommand:
                _editor.RequestCommand("tableDelete");
                break;
        }

        return ValueTask.CompletedTask;
    }
}
