namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>Options configuring predefined templates for the rich text editor.</summary>
public sealed record TemplatePluginOptions
{
    /// <summary>The predefined templates available in the templates menu.</summary>
    public IReadOnlyList<ComposerTemplate> Templates { get; set; } =
    [
        new("article", "Article Layout", "Two-column layout with lead paragraph and pull-quote.", "<p class=\"forma-composer-lead\">Lead paragraph summarizing the article.</p><p>First body paragraph with details and analysis.</p><blockquote>Notable quote highlighting a key insight.</blockquote><p>Concluding paragraph.</p>"),
        new("sidebar", "Sidebar Callout", "A highlight callout box with a title.", "<aside class=\"forma-composer-callout\"><h3>Key Takeaways</h3><ul><li>First essential point</li><li>Second essential point</li></ul></aside>"),
    ];

    /// <summary>Whether a template replaces the entire document content.</summary>
    public bool ReplaceContent { get; set; }

    /// <summary>Default options for the template plugin.</summary>
    public static TemplatePluginOptions Default => new();
}
