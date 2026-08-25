namespace Forma.UI.Blazor.Components.Composer.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Icons;

/// <summary>Contributes a read-only rendered document preview to the composer toolbar, for any editor.</summary>
public sealed class PreviewPlugin : IComposerPlugin
{
    /// <summary>The command opening a read-only rendered preview of the document.</summary>
    public const string PreviewCommand = "view.preview";

    /// <inheritdoc />
    public string Id => "preview";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.BuiltIn,
        [
            new FormaToolbarGroup
            {
                Id = "view",
                Title = "View",
                Items =
                [
                    new FormaToolbarCommand
                    {
                        Id = "preview",
                        Title = "Preview",
                        Icon = Glyph.Eye,
                        Command = PreviewCommand,
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

        // The engine registers PreviewCommand itself and claims it before any plugin is consulted.
        return ValueTask.CompletedTask;
    }
}
