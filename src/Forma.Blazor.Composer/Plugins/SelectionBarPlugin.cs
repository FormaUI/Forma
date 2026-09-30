namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>The feature plugin that toggles the formatting bar that follows the text selection.</summary>
public sealed class SelectionBarPlugin : IComposerFeaturePlugin
{
    /// <inheritdoc />
    public string Id => "selection-bar";

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
