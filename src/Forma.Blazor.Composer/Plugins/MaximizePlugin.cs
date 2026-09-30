namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>The feature plugin that toggles whether the composer fills the viewport.</summary>
public sealed class MaximizePlugin : IComposerFeaturePlugin
{
    /// <inheritdoc />
    public string Id => "maximize";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.BuiltIn,
        [
            new ToolbarGroup
            {
                Id = "view",
                Title = "View",
                Items =
                [
                    new ToolbarToggle
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
