namespace Forma.Framework.Composer.Tests.Plugins;

using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Plugins;

using Xunit;

public sealed class ComposerPluginCatalogTests
{
    private sealed class FeaturePlugin(string id, bool isAvailable = true) : IComposerFeaturePlugin
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

    private sealed class ViewPlugin(string id) : IComposerViewPlugin
    {
        public string Id => id;
        public string Kind => ComposerViews.Preview;
        public string Title => id;
        public Icon Icon => Glyph.Preview;
        public Type ViewComponent => typeof(ViewPlugin);
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private static readonly EditorPlugin Fallback = new("fallback", ComposerDocumentFormat.PlainText);

    private static PluginInstance<IComposerPlugin> CreateInstance(IComposerPlugin plugin, PluginInstance<IComposerPlugin>? owner = null)
        => new() { Plugin = plugin, Owner = owner };

    private static ValueTask<ComposerPluginCatalog> CreateAsync(params PluginInstance<IComposerPlugin>[] instances)
        => ComposerPluginCatalog.CreateAsync(instances, Fallback, TestContext.Current.CancellationToken);

    public sealed class Availability
    {
        [Fact]
        public async Task Should_drop_a_plugin_that_reports_itself_unavailable()
        {
            // Arrange
            var instance = CreateInstance(new FeaturePlugin("missing", isAvailable: false));

            // Act
            var catalog = await CreateAsync(instance);

            // Assert
            Assert.Empty(catalog.Plugins);
        }

        [Fact]
        public async Task Should_fall_back_when_no_editor_is_available()
        {
            // Arrange
            var instance = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html, isAvailable: false));

            // Act
            var catalog = await CreateAsync(instance);

            // Assert
            Assert.Equal(["fallback"], catalog.Editors.Select(editor => editor.Id));
        }

        [Fact]
        public async Task Should_keep_only_the_first_plugin_of_an_identifier_one_owner_repeats()
        {
            // Arrange
            var editor = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));

            // Act
            var catalog = await CreateAsync(
                editor,
                CreateInstance(new FeaturePlugin("tables"), editor),
                CreateInstance(new FeaturePlugin("tables"), editor));

            // Assert
            Assert.Single(catalog.GetFeaturePlugins((IComposerEditorPlugin)editor.Plugin));
        }

        [Fact]
        public async Task Should_keep_the_first_available_plugin_of_an_identifier_past_an_unavailable_one()
        {
            // Arrange
            var editor = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var standIn = new FeaturePlugin("tables");

            // Act
            var catalog = await CreateAsync(
                editor,
                CreateInstance(new FeaturePlugin("tables", isAvailable: false), editor),
                CreateInstance(standIn, editor));

            // Assert
            Assert.Same(standIn, Assert.Single(catalog.GetFeaturePlugins((IComposerEditorPlugin)editor.Plugin)));
        }

        [Fact]
        public async Task Should_keep_a_plugin_of_one_identifier_that_two_owners_each_register()
        {
            // Arrange
            var first = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var second = CreateInstance(new EditorPlugin("code", ComposerDocumentFormat.PlainText));

            // Act
            var catalog = await CreateAsync(
                first,
                CreateInstance(new FeaturePlugin("link"), first),
                second,
                CreateInstance(new FeaturePlugin("link"), second));

            // Assert
            Assert.Equal(2, catalog.Plugins.Count(plugin => plugin.Id == "link"));
        }
    }

    public sealed class Scope
    {
        [Fact]
        public async Task Should_keep_the_order_the_plugins_were_registered_in()
        {
            // Arrange
            var editor = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));

            // Act
            var catalog = await CreateAsync(
                CreateInstance(new FeaturePlugin("maximize")),
                editor,
                CreateInstance(new FeaturePlugin("tables"), editor));

            // Assert
            Assert.Equal(["maximize", "html", "tables"], catalog.Plugins.Select(plugin => plugin.Id));
        }

        [Fact]
        public async Task Should_answer_an_editor_with_the_views_registered_under_it()
        {
            // Arrange
            var first = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var second = CreateInstance(new EditorPlugin("code", ComposerDocumentFormat.PlainText));
            var catalog = await CreateAsync(
                first,
                CreateInstance(new ViewPlugin("html.preview"), first),
                second,
                CreateInstance(new ViewPlugin("code.preview"), second));

            // Act
            var views = catalog.GetViews((IComposerEditorPlugin)first.Plugin);

            // Assert
            Assert.Equal(["html.preview"], views.Select(view => view.Id));
        }

        [Fact]
        public async Task Should_offer_a_plugin_the_composer_registered_to_every_editor()
        {
            // Arrange
            var first = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var second = CreateInstance(new EditorPlugin("code", ComposerDocumentFormat.PlainText));
            var catalog = await CreateAsync(CreateInstance(new FeaturePlugin("maximize")), first, second);

            // Act
            var onFirst = catalog.GetFeaturePlugins((IComposerEditorPlugin)first.Plugin);
            var onSecond = catalog.GetFeaturePlugins((IComposerEditorPlugin)second.Plugin);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["maximize"], onFirst.Select(plugin => plugin.Id)),
                () => Assert.Equal(["maximize"], onSecond.Select(plugin => plugin.Id)));
        }

        [Fact]
        public async Task Should_offer_a_plugin_an_editor_registered_to_that_editor_alone()
        {
            // Arrange
            var first = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var second = CreateInstance(new EditorPlugin("code", ComposerDocumentFormat.PlainText));
            var catalog = await CreateAsync(first, CreateInstance(new FeaturePlugin("tables"), first), second);

            // Act
            var onSecond = catalog.GetFeaturePlugins((IComposerEditorPlugin)second.Plugin);

            // Assert
            Assert.Empty(onSecond);
        }

        [Fact]
        public async Task Should_offer_a_plugin_a_view_registered_only_while_that_view_shows()
        {
            // Arrange
            var editor = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var view = CreateInstance(new ViewPlugin("html.preview"), editor);
            var catalog = await CreateAsync(editor, view, CreateInstance(new FeaturePlugin("paper-size"), view));

            // Act
            var onEditor = catalog.GetFeaturePlugins((IComposerEditorPlugin)editor.Plugin);
            var onView = catalog.GetFeaturePlugins((IComposerEditorPlugin)editor.Plugin, (IComposerViewPlugin)view.Plugin);

            // Assert
            Assert.Multiple(
                () => Assert.Empty(onEditor),
                () => Assert.Equal(["paper-size"], onView.Select(plugin => plugin.Id)));
        }

        [Fact]
        public async Task Should_leave_what_an_editor_registered_off_a_view_showing_over_it()
        {
            // Arrange
            var editor = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var view = CreateInstance(new ViewPlugin("html.preview"), editor);
            var catalog = await CreateAsync(editor, CreateInstance(new FeaturePlugin("attach"), editor), view);

            // Act
            var onView = catalog.GetFeaturePlugins((IComposerEditorPlugin)editor.Plugin, (IComposerViewPlugin)view.Plugin);

            // Assert
            Assert.Empty(onView);
        }

        [Fact]
        public async Task Should_answer_an_identifier_with_the_surfaces_own_plugin_over_the_composers()
        {
            // Arrange
            var editor = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var own = new FeaturePlugin("attach");
            var catalog = await CreateAsync(CreateInstance(new FeaturePlugin("attach")), editor, CreateInstance(own, editor));

            // Act
            var onEditor = catalog.GetFeaturePlugins((IComposerEditorPlugin)editor.Plugin);

            // Assert
            Assert.Same(own, Assert.Single(onEditor));
        }

        [Fact]
        public async Task Should_lead_the_surface_with_the_editor_while_no_view_shows()
        {
            // Arrange
            var editor = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var catalog = await CreateAsync(CreateInstance(new FeaturePlugin("maximize")), editor, CreateInstance(new FeaturePlugin("attach"), editor));

            // Act
            var inPlay = catalog.GetSurfacePlugins((IComposerEditorPlugin)editor.Plugin);

            // Assert
            Assert.Equal(["html", "maximize", "attach"], inPlay.Select(plugin => plugin.Id));
        }

        [Fact]
        public async Task Should_lead_the_surface_with_the_view_in_the_editors_place_while_one_shows()
        {
            // Arrange
            var editor = CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html));
            var view = CreateInstance(new ViewPlugin("html.preview"), editor);
            var catalog = await CreateAsync(
                CreateInstance(new FeaturePlugin("maximize")),
                editor,
                CreateInstance(new FeaturePlugin("attach"), editor),
                view,
                CreateInstance(new FeaturePlugin("paper-size"), view));

            // Act
            var inPlay = catalog.GetSurfacePlugins((IComposerEditorPlugin)editor.Plugin, (IComposerViewPlugin)view.Plugin);

            // Assert
            Assert.Equal(["html.preview", "maximize", "paper-size"], inPlay.Select(plugin => plugin.Id));
        }
    }

    public sealed class Resolution
    {
        private static async ValueTask<ComposerPluginCatalog> CreateTwoEditorsAsync()
            => await CreateAsync(
                CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html)),
                CreateInstance(new EditorPlugin("code", ComposerDocumentFormat.PlainText)));

        [Fact]
        public async Task Should_resolve_the_explicitly_requested_editor()
        {
            // Arrange
            var catalog = await CreateTwoEditorsAsync();

            // Act
            var resolved = catalog.ResolveEditor("code", defaultId: "html", ComposerDocumentFormat.Html);

            // Assert
            Assert.Equal("code", resolved.Id);
        }

        [Fact]
        public async Task Should_resolve_the_configured_default_when_nothing_was_requested()
        {
            // Arrange
            var catalog = await CreateTwoEditorsAsync();

            // Act
            var resolved = catalog.ResolveEditor(requestedId: null, defaultId: "code", ComposerDocumentFormat.Html);

            // Assert
            Assert.Equal("code", resolved.Id);
        }

        [Fact]
        public async Task Should_resolve_by_document_format_when_neither_was_named()
        {
            // Arrange
            var catalog = await CreateTwoEditorsAsync();

            // Act
            var resolved = catalog.ResolveEditor(requestedId: null, defaultId: null, ComposerDocumentFormat.PlainText);

            // Assert
            Assert.Equal("code", resolved.Id);
        }

        [Fact]
        public async Task Should_resolve_the_first_editor_when_no_rule_matches()
        {
            // Arrange
            var catalog = await CreateAsync(CreateInstance(new EditorPlugin("html", ComposerDocumentFormat.Html)));

            // Act
            var resolved = catalog.ResolveEditor("absent", defaultId: "also-absent", ComposerDocumentFormat.PlainText);

            // Assert
            Assert.Equal("html", resolved.Id);
        }
    }
}
