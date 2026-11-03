namespace Forma.Blazor.Components.Composer;

using Forma.Blazor.Components.Composer.PlainTextEditor;
using Forma.Blazor.Components.Composer.Plugins;
using Forma.Blazor.Components.Composer.RichTextEditor;
using Forma.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.Blazor.Components.Composer.RichTextEditor.Views;
using Forma.Framework.Composer.Plugins;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>The registration of what every composer in the application is made of.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the plugins every <see cref="FormaComposer"/> is built from.</summary>
    /// <param name="services">The application's services.</param>
    /// <param name="configure">Registers the editors, views and capabilities a composer has.</param>
    public static IServiceCollection AddFormaComposer(this IServiceCollection services, Action<ComposerPluginBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton(ComposerDefinition.Create(configure));

        return services;
    }

    /// <summary>Registers the visual rich text editor.</summary>
    /// <param name="composer">The composer being described.</param>
    /// <param name="configure">Registers the editor's own plugins and views, where null registers the editor bare.</param>
    public static ComposerPluginBuilder AddRichTextEditor(
        this ComposerPluginBuilder composer,
        Action<ComposerEditorBuilder<RichTextEditorPlugin>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(composer);

        return composer.AddEditorPlugin(configure);
    }

    /// <summary>Registers the plain text editor.</summary>
    /// <param name="composer">The composer being described.</param>
    /// <param name="configure">Registers the editor's own plugins and views, where null registers the editor bare.</param>
    public static ComposerPluginBuilder AddPlainTextEditor(
        this ComposerPluginBuilder composer,
        Action<ComposerEditorBuilder<PlainTextEditorPlugin>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(composer);

        return composer.AddEditorPlugin(configure);
    }

    /// <summary>Registers the default plugins of every editor and view.</summary>
    /// <param name="composer">The composer being described.</param>
    public static ComposerPluginBuilder TryAddDefaultPlugins(this ComposerPluginBuilder composer)
    {
        ArgumentNullException.ThrowIfNull(composer);

        return composer
            .TryAddFeaturePlugin<MaximizePlugin>()
            .TryAddFeaturePlugin<ShortcutsPlugin>();
    }

    /// <summary>Registers the plain text editor's default feature plugins.</summary>
    /// <param name="editor">The editor being described.</param>
    public static ComposerEditorBuilder<PlainTextEditorPlugin> TryAddDefaultFeatures(this ComposerEditorBuilder<PlainTextEditorPlugin> editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        return editor.TryAddFeaturePlugin<FileUploadPlugin>();
    }

    /// <summary>Registers the rich text editor's default feature plugins.</summary>
    /// <param name="editor">The editor being described.</param>
    public static ComposerEditorBuilder<RichTextEditorPlugin> TryAddDefaultFeatures(this ComposerEditorBuilder<RichTextEditorPlugin> editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        return editor
            .TryAddFeaturePlugin<BasicFormattingPlugin>()
            .TryAddFeaturePlugin<ParagraphFormatPlugin>()
            .TryAddFeaturePlugin<TypographyPlugin>()
            .TryAddFeaturePlugin<ListPlugin>()
            .TryAddFeaturePlugin<AlignmentBidiPlugin>()
            .TryAddFeaturePlugin<LinkPlugin>()
            .TryAddFeaturePlugin<ImagePlugin>()
            .TryAddFeaturePlugin<VideoPlugin>()
            .TryAddFeaturePlugin<TablePlugin>()
            .TryAddFeaturePlugin<HorizontalRulePlugin>()
            .TryAddFeaturePlugin<EmojiPlugin>()
            .TryAddFeaturePlugin<SpecialCharacterPlugin>()
            .TryAddFeaturePlugin<ClipboardPlugin>()
            .TryAddFeaturePlugin<EmbedPlugin>()
            .TryAddFeaturePlugin<TemplatePlugin>()
            .TryAddFeaturePlugin<FileUploadPlugin>()
            .TryAddFeaturePlugin<SelectionBarPlugin>()
            .AddViewPlugin<RichTextPreviewViewPlugin>()
            .AddViewPlugin<RichTextSourceViewPlugin>();
    }
}
