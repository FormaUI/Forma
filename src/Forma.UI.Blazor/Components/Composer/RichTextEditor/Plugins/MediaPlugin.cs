namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Glyphs;

/// <summary>Contributes image and media insertion to the rich text editor.</summary>
public sealed class MediaPlugin(
    RichTextEditorPlugin editor,
    MediaPluginOptions? options = null) : IComposerPlugin
{
    /// <summary>The command opening the insert image dialog.</summary>
    public const string InsertMediaCommand = "media.insert";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <summary>Options configuring media capabilities.</summary>
    public MediaPluginOptions Options { get; } = options ?? MediaPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "media";

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
                        Id = "media-insert",
                        Title = "Insert Image",
                        Icon = Glyph.Image,
                        Command = InsertMediaCommand,
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

        if (command == InsertMediaCommand)
        {
            _editor.RequestMediaDialog();
        }

        return ValueTask.CompletedTask;
    }
}
