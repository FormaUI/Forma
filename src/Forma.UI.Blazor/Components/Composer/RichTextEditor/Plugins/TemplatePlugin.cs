namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Icons;

/// <summary>Contributes predefined document template insertion to the rich text editor.</summary>
public sealed class TemplatePlugin(
    RichTextEditorPlugin editor,
    TemplatePluginOptions? options = null) : IComposerPlugin
{
    /// <summary>Command prefix for inserting a specific predefined template.</summary>
    public const string InsertTemplatePrefix = "template.insert:";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <summary>Options configuring available templates and replacement behavior.</summary>
    public TemplatePluginOptions Options { get; } = options ?? TemplatePluginOptions.Default;

    /// <inheritdoc />
    public string Id => "template";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.BuiltIn,
        [
            new FormaToolbarGroup
            {
                Id = "templates",
                Title = "Templates",
                Items =
                [
                    new FormaToolbarMenu
                    {
                        Id = "template-menu",
                        Title = "Templates",
                        Icon = Glyph.Copy,
                        Options = [.. Options.Templates.Select(t => new FormaToolbarOption
                        {
                            Id = $"tpl-{t.Id}",
                            Title = t.Title,
                            Command = $"{InsertTemplatePrefix}{t.Id}",
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

        if (command.StartsWith(InsertTemplatePrefix, StringComparison.Ordinal))
        {
            var templateId = command[InsertTemplatePrefix.Length..];
            var template = Options.Templates.FirstOrDefault(t => t.Id == templateId);

            if (template is not null)
            {
                var execCommand = Options.ReplaceContent ? "replaceContent" : "insertHtml";
                _editor.RequestCommand(execCommand, template.Html);
            }
        }

        return ValueTask.CompletedTask;
    }
}
