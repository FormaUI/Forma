namespace Forma.Blazor.Browser.Components.Client.Checks.Composer;

using System.Globalization;

using Forma.Blazor.Components.Composer.RichTextEditor;
using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>A plugin of a host's own: a module and a stylesheet it needs, a marker beside the second block that brings the caret to it, and a picture it inserts.</summary>
/// <param name="editor">The rich text editor whose canvas the marker stands beside.</param>
public sealed class CheckComposerPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    private const string MarkCommand = "check.mark";
    private const string InsertCommand = "check.insert";
    private const string RevealPrefix = "check.reveal-";

    private IComposerGutterMarker? _marker;

    /// <inheritdoc />
    public string Id => "check";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Plugins,
        [
            new ToolbarCommand { Id = "check-mark", Title = "Mark Second", Icon = Glyph.Flag, Command = MarkCommand },
            new ToolbarCommand { Id = "check-insert", Title = "Insert Picture", Icon = Glyph.Image, Command = InsertCommand },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> CommandPrefixes => [RevealPrefix];

    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets =>
    [
        new(ComposerAssetKind.Module, "data:text/javascript,window.formaCheckModule=1"),
        new(ComposerAssetKind.Stylesheet, "data:text/css,.forma-check-style{}"),
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

        if (command == MarkCommand)
        {
            _marker ??= context.Regions.CreateGutterMarker(1, Glyph.Flag, "Second", RevealPrefix + "1");
        }
        else if (command == InsertCommand)
        {
            await interaction.TryInsertAsync(new ResourceInsertion { Source = "images/check.svg", Name = "check.svg", Kind = "image", Embed = true, Link = false }).ConfigureAwait(false);
        }
        else if (command.StartsWith(RevealPrefix, StringComparison.Ordinal))
        {
            editor.RequestCommand("revealBlock", int.Parse(command[RevealPrefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture));
        }
    }
}
