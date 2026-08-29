namespace Forma.UI.Blazor.Components.Composer.Plugins;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>Contributes attaching uploaded files to the document, for any editor.</summary>
public sealed class FileUploadPlugin(FileUploadPluginOptions? options = null) : IComposerFeaturePlugin
{
    /// <summary>Options configuring which files may be attached.</summary>
    public FileUploadPluginOptions Options { get; } = options ?? FileUploadPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "file-upload";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.BuiltIn,
        [
            new FormaToolbarGroup
            {
                Id = "attachments",
                Title = "Attachments",
                Items =
                [
                    new FormaToolbarCommand
                    {
                        Id = "attach",
                        Title = "Attach Files",
                        Icon = Glyph.Attachment,
                        Command = ComposerCommands.Attach,
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    // Attachments belong to the document rather than to any one editor, so the engine runs the command and this never sees it.
    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);

        return ValueTask.CompletedTask;
    }
}
