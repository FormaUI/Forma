namespace Forma.Framework.Composer.Tests.Plugins;

using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Plugins;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ComposerDefinitionTests
{
    public sealed class StubEditor : IComposerEditorPlugin
    {
        public string Id => "stub-editor";
        public string Title => "Stub";
        public Icon Icon => Glyph.File;
        public ComposerDocumentFormat Format => ComposerDocumentFormat.PlainText;
        public Type EditorComponent => typeof(StubEditor);
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class SecondEditor : IComposerEditorPlugin
    {
        public string Id => "second-editor";
        public string Title => "Second";
        public Icon Icon => Glyph.File;
        public ComposerDocumentFormat Format => ComposerDocumentFormat.Html;
        public Type EditorComponent => typeof(SecondEditor);
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class PreviewView : IComposerViewPlugin
    {
        public string Id => "preview";
        public string EditorId => "stub-editor";
        public string Kind => ComposerViews.Preview;
        public string Title => "Preview";
        public Icon Icon => Glyph.File;
        public Type ViewComponent => typeof(PreviewView);
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class EditorBoundPlugin(StubEditor editor) : IComposerFeaturePlugin
    {
        public StubEditor Editor { get; } = editor;
        public string Id => "editor-bound";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class ViewBoundPlugin : IComposerFeaturePlugin
    {
        public string Id => "view-bound";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class CountOptions
    {
        public int Count { get; set; } = 1;
    }

    public sealed class CountingPlugin(CountOptions? options = null) : IComposerFeaturePlugin, IPluginConfigurable<CountOptions>
    {
        public CountOptions Options { get; } = options ?? new CountOptions();
        public string Id => "counting";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private static ServiceProvider CreateServices()
        => new ServiceCollection().BuildServiceProvider();

    public sealed class Activation
    {
        [Fact]
        public void Should_build_the_plugins_in_the_order_they_were_registered()
        {
            // Arrange
            using var services = CreateServices();
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>()
                .AddFeaturePlugin<EditorBoundPlugin>());

            // Act
            var plugins = definition.Activate(services).Plugins;

            // Assert
            Assert.Collection(
                plugins,
                plugin => Assert.IsType<StubEditor>(plugin),
                plugin => Assert.IsType<EditorBoundPlugin>(plugin));
        }

        [Fact]
        public void Should_hand_every_plugin_the_one_editor_this_composer_built()
        {
            // Arrange
            using var services = CreateServices();
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>(editor => editor.AddFeaturePlugin<EditorBoundPlugin>()));

            // Act
            var plugins = definition.Activate(services).Plugins;

            // Assert
            Assert.Same(plugins.OfType<StubEditor>().Single(), plugins.OfType<EditorBoundPlugin>().Single().Editor);
        }

        [Fact]
        public void Should_build_a_separate_scope_for_every_composer()
        {
            // Arrange
            using var services = CreateServices();
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>()
                .AddFeaturePlugin<EditorBoundPlugin>());

            // Act
            var first = definition.Activate(services).Plugins;
            var second = definition.Activate(services).Plugins;

            // Assert
            Assert.Multiple(
                () => Assert.NotSame(first.OfType<StubEditor>().Single(), second.OfType<StubEditor>().Single()),
                () => Assert.NotSame(first.OfType<EditorBoundPlugin>().Single(), second.OfType<EditorBoundPlugin>().Single()));
        }

        [Fact]
        public void Should_create_the_editor_the_registration_names_a_factory_for()
        {
            // Arrange
            using var services = CreateServices();
            var editor = new StubEditor();
            var definition = ComposerDefinition.Create(composer => composer.AddEditorPlugin(_ => editor));

            // Act
            var plugins = definition.Activate(services).Plugins;

            // Assert
            Assert.Same(editor, Assert.Single(plugins));
        }

        [Fact]
        public void Should_activate_nothing_for_an_empty_description()
        {
            // Arrange
            using var services = CreateServices();

            // Act
            var plugins = ComposerDefinition.Empty.Activate(services).Plugins;

            // Assert
            Assert.Empty(plugins);
        }
    }

    public sealed class Composition
    {
        [Fact]
        public void Should_lay_an_editor_out_ahead_of_its_views_and_their_plugins()
        {
            // Arrange
            using var services = CreateServices();
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>(editor => editor
                    .AddFeaturePlugin<EditorBoundPlugin>()
                    .AddViewPlugin<PreviewView>(view => view.AddFeaturePlugin<ViewBoundPlugin>()))
                .AddEditorPlugin<SecondEditor>());

            // Act
            var names = definition.Activate(services).Plugins.Select(plugin => plugin.Id).ToList();

            // Assert
            Assert.Equal(["stub-editor", "editor-bound", "preview", "view-bound", "second-editor"], names);
        }

        [Fact]
        public void Should_configure_a_plugin_registered_under_an_editor()
        {
            // Arrange
            using var services = CreateServices();
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>(editor => editor
                    .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 6)));

            // Act
            var plugin = definition.Activate(services).Plugins.OfType<CountingPlugin>().Single();

            // Assert
            Assert.Equal(6, plugin.Options.Count);
        }

        [Fact]
        public void Should_fall_back_on_a_default_the_editor_registers_nothing_for()
        {
            // Arrange
            using var services = CreateServices();
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>(editor => editor.TryAddFeaturePlugin<CountingPlugin>()));

            // Act
            var names = definition.Activate(services).Plugins.Select(plugin => plugin.Id).ToList();

            // Assert
            Assert.Equal(["stub-editor", "counting"], names);
        }

        [Fact]
        public void Should_keep_what_the_composer_registered_over_a_default_it_asked_for()
        {
            // Arrange
            using var services = CreateServices();
            var definition = ComposerDefinition.Create(composer => composer
                .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 4)
                .TryAddFeaturePlugin<CountingPlugin>());

            // Act
            var plugin = Assert.IsType<CountingPlugin>(Assert.Single(definition.Activate(services).Plugins));

            // Assert
            Assert.Equal(4, plugin.Options.Count);
        }
    }
}
