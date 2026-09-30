namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Dialogs;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Plugins;
using Forma.Framework.Toolbar;

/// <summary>The composer plugin that browses the host's stored files and inserts the one chosen.</summary>
public sealed class ExplorerPlugin(ExplorerPluginOptions? options = null) : IComposerFeaturePlugin, IPluginConfigurable<ExplorerPluginOptions>
{
    /// <summary>The command opening the stored files dialog.</summary>
    public const string BrowseCommand = "explorer.browse";

    /// <summary>The options configuring what the plugin browses, takes in, and inserts.</summary>
    public ExplorerPluginOptions Options { get; } = options ?? ExplorerPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "explorer";

    /// <inheritdoc />
    public Type LocalizationSourceType => typeof(FormaComposerExplorerDialog);

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
    public async ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command != BrowseCommand)
        {
            return;
        }

        await interaction.OpenDialogAsync(new ComposerInsertionRequest
        {
            Body = typeof(FormaComposerExplorerDialog),
            Size = ComposerDialogSize.Large,
            Parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [nameof(FormaComposerExplorerDialog.Options)] = Options,
            },
        });
    }
}
