namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Glyphs;

/// <summary>Contributes typographic and mathematical symbol insertion to the rich text editor.</summary>
public sealed class SpecialCharacterPlugin(
    RichTextEditorPlugin editor,
    SpecialCharacterPluginOptions? options = null) : IComposerPlugin
{
    /// <summary>Command prefix for inserting a specific special character.</summary>
    public const string InsertCharacterPrefix = "special-character.insert:";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <summary>Options configuring available symbols.</summary>
    public SpecialCharacterPluginOptions Options { get; } = options ?? SpecialCharacterPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "special-character";

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
                        Id = "special-character",
                        Title = "Special Character",
                        Icon = Glyph.Font,
                        Options = [.. Options.Characters.Select(c => new FormaToolbarOption
                        {
                            Id = $"char-{c}",
                            Title = c,
                            Command = $"{InsertCharacterPrefix}{c}",
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

        if (command.StartsWith(InsertCharacterPrefix, StringComparison.Ordinal))
        {
            var character = command[InsertCharacterPrefix.Length..];
            if (!string.IsNullOrEmpty(character))
            {
                _editor.RequestCommand("insertText", character);
            }
        }

        return ValueTask.CompletedTask;
    }
}
