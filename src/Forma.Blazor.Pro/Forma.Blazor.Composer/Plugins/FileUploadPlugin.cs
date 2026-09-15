namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Plugins;
using Forma.Framework.Toolbar;

/// <summary>Contributes attaching uploaded files to the document, for any editor.</summary>
public sealed class FileUploadPlugin(FileUploadPluginOptions? options = null) : IComposerFeaturePlugin, IPluginConfigurable<FileUploadPluginOptions>
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

}
