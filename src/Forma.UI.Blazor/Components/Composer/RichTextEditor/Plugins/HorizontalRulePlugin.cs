namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>Contributes horizontal rule separator insertion to the rich text editor.</summary>
public sealed class HorizontalRulePlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command inserting a horizontal divider line at the active caret position.</summary>
    public const string InsertRuleCommand = "insert.horizontal-rule";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <inheritdoc />
    public string Id => "horizontal-rule";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new FormaToolbarGroup
            {
                Id = "insert",
                Title = "Insert",
                Items =
                [
                    new FormaToolbarCommand
                    {
                        Id = "horizontal-rule",
                        Title = "Horizontal Line",
                        Icon = Glyph.Minus,
                        Command = InsertRuleCommand,
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);

        if (command == InsertRuleCommand)
        {
            _editor.RequestCommand("insertHorizontalRule");
        }

        return ValueTask.CompletedTask;
    }
}
