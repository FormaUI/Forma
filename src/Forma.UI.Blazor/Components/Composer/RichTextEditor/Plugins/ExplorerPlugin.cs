namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>Contributes browsing the host's stored files, and inserting whichever one is chosen.</summary>
public sealed class ExplorerPlugin(
    RichTextEditorPlugin editor,
    ExplorerPluginOptions? options = null) : IComposerFeaturePlugin
{
    /// <summary>The command opening the stored files dialog.</summary>
    public const string BrowseCommand = "explorer.browse";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <summary>Options configuring what is browsed, taken in, and inserted.</summary>
    public ExplorerPluginOptions Options { get; } = options ?? ExplorerPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "explorer";

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
                        Id = "explorer-browse",
                        Title = "Browse Files",
                        Icon = Glyph.FolderOpen,
                        Command = BrowseCommand,
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

        if (command == BrowseCommand)
        {
            _editor.RequestExplorerDialog(Options);
        }

        return ValueTask.CompletedTask;
    }
}
