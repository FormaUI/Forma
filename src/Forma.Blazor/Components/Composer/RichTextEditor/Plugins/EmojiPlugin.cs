namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Features.Composer.Assets;
using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;
using Forma.Framework.Toolbar;

/// <summary>Contributes emoji character insertion to the rich text editor.</summary>
public sealed class EmojiPlugin(
    RichTextEditorPlugin editor,
    EmojiPluginOptions? options = null) : IComposerFeaturePlugin
{
    /// <summary>Command prefix for inserting a specific emoji character.</summary>
    public const string InsertEmojiPrefix = "emoji.insert:";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <summary>Options configuring the emoji picker.</summary>
    public EmojiPluginOptions Options { get; } = options ?? EmojiPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "emoji";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<string> CommandPrefixes => [InsertEmojiPrefix];

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
                    new FormaToolbarCustomItem
                    {
                        Id = "emoji",
                        Title = "Emoji",
                        Component = typeof(FormaComposerEmojiButton),
                        Parameters = new Dictionary<string, object> { ["PickerOptions"] = Options.Picker },
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

        if (command.StartsWith(InsertEmojiPrefix, StringComparison.Ordinal))
        {
            var emoji = command[InsertEmojiPrefix.Length..];
            if (!string.IsNullOrEmpty(emoji))
            {
                _editor.RequestCommand("insertText", emoji);
            }
        }

        return ValueTask.CompletedTask;
    }
}
