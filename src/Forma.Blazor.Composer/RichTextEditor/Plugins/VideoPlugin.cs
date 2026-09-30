namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes video embed insertion to the rich text editor.</summary>
public sealed class VideoPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command opening the insert video dialog.</summary>
    public const string InsertVideoCommand = "video.insert";

    private readonly RichTextEditorPlugin _editorPlugin = editor;

    /// <inheritdoc />
    public string Id => "video";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new ToolbarGroup
            {
                Id = "media",
                Title = "Media",
                Items =
                [
                    new ToolbarCommand
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
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command == InsertVideoCommand)
        {
            _editorPlugin.RequestVideoDialog();
        }

        return ValueTask.CompletedTask;
    }
}
