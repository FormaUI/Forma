namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>Contributes video embed insertion to the rich text editor.</summary>
public sealed class VideoPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command opening the insert video embed dialog.</summary>
    public const string InsertVideoCommand = "video.insert";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <inheritdoc />
    public string Id => "video";

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
                        Id = "video-insert",
                        Title = "Insert Video",
                        Icon = Glyph.Video,
                        Command = InsertVideoCommand,
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

        if (command == InsertVideoCommand)
        {
            _editor.RequestVideoDialog();
        }

        return ValueTask.CompletedTask;
    }
}
