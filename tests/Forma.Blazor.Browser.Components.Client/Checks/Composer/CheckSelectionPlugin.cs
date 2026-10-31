namespace Forma.Blazor.Browser.Components.Client.Checks.Composer;

using System.Globalization;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>A plugin of a host's own that shows the selection the editor last reported in the composer's status strip.</summary>
public sealed class CheckSelectionPlugin : IComposerFeaturePlugin
{
    private const string ShowCommand = "check.selection";

    private IComposerStatusItem? _readout;

    /// <inheritdoc />
    public string Id => "check-selection";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
        [new(ComposerRows.Plugins, [new ToolbarCommand { Id = "check-selection", Title = "Show Selection", Icon = Glyph.Select, Command = ShowCommand }])];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var text = string.Create(CultureInfo.InvariantCulture, $"{context.Selection.Start}-{context.Selection.End}");

        if (_readout is null)
        {
            _readout = context.Regions.CreateStatusItem(text);
        }
        else
        {
            _readout.Text = text;
        }

        return ValueTask.CompletedTask;
    }
}
