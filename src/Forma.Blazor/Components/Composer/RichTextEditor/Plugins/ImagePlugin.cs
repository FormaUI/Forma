namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Features.Composer.Assets;
using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes image insertion to the rich text editor.</summary>
public sealed class ImagePlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command opening the insert image dialog.</summary>
    public const string InsertImageCommand = "image.insert";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <inheritdoc />
    public string Id => "image";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new FormaToolbarGroup
            {
                Id = "media",
                Title = "Media",
                Items =
                [
                    new FormaToolbarCommand
                    {
                        Id = "image-insert",
                        Title = "Insert Image",
                        Icon = Glyph.Image,
                        Command = InsertImageCommand,
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerState state, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command == InsertImageCommand)
        {
            _editor.RequestImageDialog();
        }

        return ValueTask.CompletedTask;
    }
}
