namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using System.Text;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes typographic and mathematical symbol insertion to the rich text editor.</summary>
public sealed class SpecialCharacterPlugin(
    RichTextEditorPlugin editor,
    SpecialCharacterPluginOptions? options = null) : IComposerFeaturePlugin
{
    /// <summary>Command prefix for inserting a specific special character.</summary>
    public const string InsertCharacterPrefix = "special-character.insert:";

    private readonly RichTextEditorPlugin _editorPlugin = editor;

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
                        Icon = new Rune('Ω'),
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
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command.StartsWith(InsertCharacterPrefix, StringComparison.Ordinal))
        {
            var character = command[InsertCharacterPrefix.Length..];
            if (!string.IsNullOrEmpty(character))
            {
                _editorPlugin.RequestCommand("insertText", character);
            }
        }

        return ValueTask.CompletedTask;
    }
}
