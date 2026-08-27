namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>Contributes declarative host-resolved embed placeholder insertion to the rich text editor.</summary>
public sealed class EmbedPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command opening the insert embed dialog.</summary>
    public const string InsertEmbedCommand = "embed.insert";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <inheritdoc />
    public string Id => "embed";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Plugins,
        [
            new FormaToolbarGroup
            {
                Id = "embed",
                Title = "Embed",
                Items =
                [
                    new FormaToolbarCommand
                    {
                        Id = "embed-insert",
                        Title = "Insert Embed",
                        Icon = Glyph.TreeStructure,
                        Command = InsertEmbedCommand,
                    },
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

        if (command == InsertEmbedCommand)
        {
            _editor.RequestEmbedDialog();
        }

        return ValueTask.CompletedTask;
    }
}
