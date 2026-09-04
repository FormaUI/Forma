namespace Forma.Framework.Composer.Tests.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;

using Xunit;

public sealed class ComposerPluginCatalogTests
{
    private sealed class FeaturePlugin(string id, bool isAvailable = true) : IComposerPlugin
    {
        public string Id => id;
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(isAvailable);
    }

    private sealed class EditorPlugin(string id, ComposerDocumentFormat format, bool isAvailable = true) : IComposerEditorPlugin
    {
        public string Id => id;
        public string Title => id;
        public Icon Icon => Glyph.File;
        public ComposerDocumentFormat Format => format;
        public Type EditorComponent => typeof(EditorPlugin);
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(isAvailable);
    }

    private sealed class ViewPlugin(string id, string editorId) : IComposerViewPlugin
    {
        public string Id => id;
        public string EditorId => editorId;
        public string Kind => ComposerViews.Preview;
        public string Title => id;
        public Icon Icon => Glyph.Preview;
        public Type ViewComponent => typeof(ViewPlugin);
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private static readonly EditorPlugin Fallback = new("fallback", ComposerDocumentFormat.PlainText);

    [Fact]
    public async Task Should_split_views_from_editors_and_feature_plugins_and_answer_them_by_editor()
    {
        // Arrange
        IComposerPlugin[] candidates =
        [
            new EditorPlugin("html", ComposerDocumentFormat.Html),
            new ViewPlugin("html.preview", "html"),
            new ViewPlugin("markdown.preview", "markdown"),
            new FeaturePlugin("tables"),
        ];

        // Act
        var catalog = await CreateAsync(candidates);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["html"], catalog.Editors.Select(e => e.Id)),
            () => Assert.Equal(["html.preview", "markdown.preview"], catalog.Views.Select(v => v.Id)),
            () => Assert.Equal(["tables"], catalog.FeaturePlugins.Select(p => p.Id)),
            () => Assert.Equal(["html.preview"], catalog.GetViewsForEditor("html").Select(v => v.Id)),
            () => Assert.Equal(["html", "html.preview", "markdown.preview", "tables"], catalog.Plugins.Select(p => p.Id)));
    }

    private static ValueTask<ComposerPluginCatalog> CreateAsync(IEnumerable<IComposerPlugin> candidates)
        => ComposerPluginCatalog.CreateAsync(candidates, Fallback, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Should_split_editors_from_feature_plugins()
    {
        // Arrange
        IComposerPlugin[] candidates = [new EditorPlugin("html", ComposerDocumentFormat.Html), new FeaturePlugin("tables")];

        // Act
        var catalog = await CreateAsync(candidates);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["html"], catalog.Editors.Select(e => e.Id)),
            () => Assert.Equal(["tables"], catalog.FeaturePlugins.Select(p => p.Id)));
    }

    [Fact]
    public async Task Should_drop_a_plugin_that_reports_itself_unavailable()
    {
        // Arrange
        IComposerPlugin[] candidates = [new FeaturePlugin("missing", isAvailable: false)];

        // Act
        var catalog = await CreateAsync(candidates);

        // Assert
        Assert.Empty(catalog.FeaturePlugins);
    }

    [Fact]
    public async Task Should_keep_only_the_first_plugin_of_a_repeated_identifier()
    {
        // Arrange
        IComposerPlugin[] candidates = [new FeaturePlugin("tables"), new FeaturePlugin("tables")];

        // Act
        var catalog = await CreateAsync(candidates);

        // Assert
        Assert.Single(catalog.FeaturePlugins);
    }

    [Fact]
    public async Task Should_fall_back_when_no_editor_is_available()
    {
        // Arrange
        IComposerPlugin[] candidates = [new EditorPlugin("html", ComposerDocumentFormat.Html, isAvailable: false)];

        // Act
        var catalog = await CreateAsync(candidates);

        // Assert
        Assert.Equal(["fallback"], catalog.Editors.Select(e => e.Id));
    }

    [Fact]
    public async Task Should_resolve_the_explicitly_requested_editor()
    {
        // Arrange
        IComposerPlugin[] candidates = [new EditorPlugin("html", ComposerDocumentFormat.Html), new EditorPlugin("code", ComposerDocumentFormat.PlainText)];
        var catalog = await CreateAsync(candidates);

        // Act
        var resolved = catalog.ResolveEditor("code", defaultId: "html", ComposerDocumentFormat.Html);

        // Assert
        Assert.Equal("code", resolved.Id);
    }

    [Fact]
    public async Task Should_resolve_the_configured_default_when_nothing_was_requested()
    {
        // Arrange
        IComposerPlugin[] candidates = [new EditorPlugin("html", ComposerDocumentFormat.Html), new EditorPlugin("code", ComposerDocumentFormat.PlainText)];
        var catalog = await CreateAsync(candidates);

        // Act
        var resolved = catalog.ResolveEditor(requestedId: null, defaultId: "code", ComposerDocumentFormat.Html);

        // Assert
        Assert.Equal("code", resolved.Id);
    }

    [Fact]
    public async Task Should_resolve_by_document_format_when_neither_was_named()
    {
        // Arrange
        IComposerPlugin[] candidates = [new EditorPlugin("html", ComposerDocumentFormat.Html), new EditorPlugin("code", ComposerDocumentFormat.PlainText)];
        var catalog = await CreateAsync(candidates);

        // Act
        var resolved = catalog.ResolveEditor(requestedId: null, defaultId: null, ComposerDocumentFormat.PlainText);

        // Assert
        Assert.Equal("code", resolved.Id);
    }

    [Fact]
    public async Task Should_resolve_the_first_editor_when_no_rule_matches()
    {
        // Arrange
        IComposerPlugin[] candidates = [new EditorPlugin("html", ComposerDocumentFormat.Html)];
        var catalog = await CreateAsync(candidates);

        // Act
        var resolved = catalog.ResolveEditor("absent", defaultId: "also-absent", ComposerDocumentFormat.PlainText);

        // Assert
        Assert.Equal("html", resolved.Id);
    }

    [Fact]
    public async Task Should_list_editors_before_feature_plugins()
    {
        // Arrange
        IComposerPlugin[] candidates = [new FeaturePlugin("tables"), new EditorPlugin("html", ComposerDocumentFormat.Html)];

        // Act
        var catalog = await CreateAsync(candidates);

        // Assert
        Assert.Equal(["html", "tables"], catalog.Plugins.Select(p => p.Id));
    }
}
