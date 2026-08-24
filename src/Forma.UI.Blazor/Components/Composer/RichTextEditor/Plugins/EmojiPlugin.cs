namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Glyphs;

/// <summary>Contributes emoji character insertion to the rich text editor.</summary>
public sealed class EmojiPlugin(
    RichTextEditorPlugin editor,
    EmojiPluginOptions? options = null) : IComposerPlugin
{
    /// <summary>Command prefix for inserting a specific emoji character.</summary>
    public const string InsertEmojiPrefix = "emoji.insert:";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <summary>Options configuring available emojis.</summary>
    public EmojiPluginOptions Options { get; } = options ?? EmojiPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "emoji";

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
                    new FormaToolbarMenu
                    {
                        Id = "emoji",
                        Title = "Emoji",
                        Icon = Glyph.Comment,
                        Options = [.. Options.Emojis.Select(e => new FormaToolbarOption
                        {
                            Id = $"emoji-{e}",
                            Title = e,
                            Command = $"{InsertEmojiPrefix}{e}",
                        })],
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets => [];

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
