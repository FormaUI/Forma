namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes horizontal rule separator insertion to the rich text editor.</summary>
public sealed class HorizontalRulePlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command inserting a horizontal divider line at the active caret position.</summary>
    public const string InsertRuleCommand = "insert.horizontal-rule";

    private readonly RichTextEditorPlugin _editorPlugin = editor;

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
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command == InsertRuleCommand)
        {
            _editorPlugin.RequestCommand("insertHorizontalRule");
        }

        return ValueTask.CompletedTask;
    }
}
