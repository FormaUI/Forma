namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes a toggle showing or hiding the formatting bar that follows the text selection, for any editor.</summary>
public sealed class SelectionBarPlugin : IComposerFeaturePlugin
{
    /// <inheritdoc />
    public string Id => "selection-bar";

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
                        Id = "selection-bar",
                        Title = "Selection Bar",
                        Icon = Glyph.Select,
                        Command = ComposerCommands.SelectionBar,
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);
}
