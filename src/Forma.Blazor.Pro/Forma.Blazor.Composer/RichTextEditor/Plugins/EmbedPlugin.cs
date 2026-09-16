namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes declarative host-resolved embed placeholder insertion to the rich text editor.</summary>
public sealed class EmbedPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command opening the insert embed dialog.</summary>
    public const string InsertEmbedCommand = "embed.insert";

    private readonly RichTextEditorPlugin _editorPlugin = editor;

    /// <inheritdoc />
    public string Id => "embed";

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
                        Icon = Glyph.FileCode,
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
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command == InsertEmbedCommand)
        {
            _editorPlugin.RequestEmbedDialog();
        }

        return ValueTask.CompletedTask;
    }
}
