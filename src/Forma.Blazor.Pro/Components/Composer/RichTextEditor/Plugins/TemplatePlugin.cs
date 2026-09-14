namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes predefined document template insertion to the rich text editor.</summary>
public sealed class TemplatePlugin(
    RichTextEditorPlugin editor,
    TemplatePluginOptions? options = null) : IComposerFeaturePlugin
{
    /// <summary>Command prefix for inserting a specific predefined template.</summary>
    public const string InsertTemplatePrefix = "template.insert:";

    private readonly RichTextEditorPlugin _editorPlugin = editor;

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
                        Icon = Glyph.Layout,
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
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command.StartsWith(InsertTemplatePrefix, StringComparison.Ordinal))
        {
            var templateId = command[InsertTemplatePrefix.Length..];
            var template = Options.Templates.FirstOrDefault(t => t.Id == templateId);

            if (template is not null)
            {
                var execCommand = Options.ReplaceContent ? "replaceContent" : "insertHtml";
                _editorPlugin.RequestCommand(execCommand, template.Html);
            }
        }

        return ValueTask.CompletedTask;
    }
}
