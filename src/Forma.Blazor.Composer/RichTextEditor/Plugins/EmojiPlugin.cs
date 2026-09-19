namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Plugins;
using Forma.Framework.Toolbar;

/// <summary>Contributes emoji character insertion to the rich text editor.</summary>
public sealed class EmojiPlugin(
    RichTextEditorPlugin editor,
    EmojiPluginOptions? options = null) : IComposerFeaturePlugin, IPluginConfigurable<EmojiPluginOptions>
{
    /// <summary>Command prefix for inserting a specific emoji character.</summary>
    public const string InsertEmojiPrefix = "emoji.insert:";

    private readonly RichTextEditorPlugin _editorPlugin = editor;

    /// <summary>Options configuring the emoji picker.</summary>
    public EmojiPluginOptions Options { get; } = options ?? EmojiPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "emoji";

    /// <inheritdoc />
    public IReadOnlyList<string> CommandPrefixes => [InsertEmojiPrefix];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new ToolbarGroup
            {
                Id = "insert",
                Title = "Insert",
                Items =
                [
                    new ToolbarCustomItem
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
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command.StartsWith(InsertEmojiPrefix, StringComparison.Ordinal))
        {
            var emoji = command[InsertEmojiPrefix.Length..];
            if (!string.IsNullOrEmpty(emoji))
            {
                _editorPlugin.RequestCommand("insertText", emoji);
            }
        }

        return ValueTask.CompletedTask;
    }
}
