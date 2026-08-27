namespace Forma.UI.Blazor.Components.Composer.Plugins;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>Contributes a full-viewport maximized editing mode toggle to the composer toolbar, for any editor.</summary>
public sealed class MaximizePlugin : IComposerFeaturePlugin
{
    /// <summary>The command toggling full-viewport maximized editor state.</summary>
    public const string MaximizeCommand = "view.maximize";

    /// <inheritdoc />
    public string Id => "maximize";

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
                    new FormaToolbarToggle
                    {
                        Id = "maximize",
                        Title = "Maximize",
                        AlternateTitle = "Minimize",
                        Icon = Glyph.Maximize,
                        AlternateIcon = Glyph.Minimize,
                        Command = MaximizeCommand,
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

        // The engine registers MaximizeCommand itself and claims it before any plugin is consulted.
        return ValueTask.CompletedTask;
    }
}
