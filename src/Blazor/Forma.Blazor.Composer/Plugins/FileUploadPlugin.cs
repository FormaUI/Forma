namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Plugins;
using Forma.Framework.Toolbar;

/// <summary>The feature plugin that attaches uploaded files to the document.</summary>
public sealed class FileUploadPlugin(FileUploadPluginOptions? options = null) : IComposerFeaturePlugin, IPluginConfigurable<FileUploadPluginOptions>
{
    /// <summary>The options of the plugin.</summary>
    public FileUploadPluginOptions Options { get; } = options ?? FileUploadPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "file-upload";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.BuiltIn,
        [
            new ToolbarGroup
            {
                Id = "attachments",
                Title = "Attachments",
                Items =
                [
                    new ToolbarCommand
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

}
