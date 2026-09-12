namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes a full-viewport maximized editing mode toggle to the composer toolbar, for any editor.</summary>
public sealed class MaximizePlugin : IComposerFeaturePlugin
{
    /// <inheritdoc />
    public string Id => "maximize";

    /// <inheritdoc />
    public bool AppliesToViews => true;

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
                        Command = ComposerCommands.Maximize,
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);
}
