namespace Forma.Framework.Composer.Tests.Plugins;

using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;

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

    public sealed class EditorBoundPlugin(StubEditor editor) : IComposerFeaturePlugin
    {
        public StubEditor Editor { get; } = editor;
        public string Id => "editor-bound";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class AlsoEditorBoundPlugin(StubEditor editor) : IComposerFeaturePlugin
    {
        public StubEditor Editor { get; } = editor;
        public string Id => "also-editor-bound";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class Clock;

    public sealed class ServiceBoundPlugin(Clock clock) : IComposerFeaturePlugin
    {
        public Clock Clock { get; } = clock;
        public string Id => "service-bound";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class CountOptions
    {
        public int Count { get; set; } = 1;
    }

    public sealed class CountingPlugin(CountOptions? options = null) : IComposerFeaturePlugin, IComposerConfigurable<CountOptions>
    {
        public CountOptions Options { get; } = options ?? new CountOptions();
        public string Id => "counting";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class NeedyPlugin(Uri address) : IComposerFeaturePlugin
    {
        public Uri Address { get; } = address;
        public string Id => "needy";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class AmbiguousPlugin : IComposerFeaturePlugin
    {
        public AmbiguousPlugin()
        {
        }

        public AmbiguousPlugin(Clock clock)
        {
            Clock = clock;
        }

        public Clock? Clock { get; }
        public string Id => "ambiguous";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    public sealed class ClosablePlugin : IComposerFeaturePlugin, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public string Id => "closable";
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);

        public void Dispose()
            => IsDisposed = true;
    }

    private static ServiceProvider NoServices()
        => new ServiceCollection().BuildServiceProvider();

    public sealed class Activation
    {
        [Fact]
        public void Should_build_the_plugins_in_the_order_they_were_registered()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>()
                .AddFeaturePlugin<EditorBoundPlugin>());

            // Act
            var plugins = definition.Activate(NoServices()).Plugins;

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
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>()
                .AddFeaturePlugin<EditorBoundPlugin>()
                .AddFeaturePlugin<AlsoEditorBoundPlugin>());

            // Act
            var plugins = definition.Activate(NoServices()).Plugins;

            // Assert
            var editor = plugins.OfType<StubEditor>().Single();
            Assert.Multiple(
                () => Assert.Same(editor, plugins.OfType<EditorBoundPlugin>().Single().Editor),
                () => Assert.Same(editor, plugins.OfType<AlsoEditorBoundPlugin>().Single().Editor));
        }

        [Fact]
        public void Should_build_a_separate_set_for_every_composer()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>()
                .AddFeaturePlugin<EditorBoundPlugin>());

            // Act
            var first = definition.Activate(NoServices()).Plugins;
            var second = definition.Activate(NoServices()).Plugins;

            // Assert
            Assert.Multiple(
                () => Assert.NotSame(first.OfType<StubEditor>().Single(), second.OfType<StubEditor>().Single()),
                () => Assert.NotSame(first.OfType<EditorBoundPlugin>().Single(), second.OfType<EditorBoundPlugin>().Single()));
        }

        [Fact]
        public void Should_resolve_what_no_plugin_supplies_from_the_host()
        {
            // Arrange
            var clock = new Clock();
            var services = new ServiceCollection().AddSingleton(clock).BuildServiceProvider();
            var definition = ComposerDefinition.Create(composer => composer.AddFeaturePlugin<ServiceBoundPlugin>());

            // Act
            var plugins = definition.Activate(services).Plugins;

            // Assert
            Assert.Same(clock, plugins.OfType<ServiceBoundPlugin>().Single().Clock);
        }

        [Fact]
        public void Should_create_the_plugin_the_registration_names_a_factory_for()
        {
            // Arrange
            var editor = new StubEditor();
            var definition = ComposerDefinition.Create(composer => composer.AddEditorPlugin(_ => editor));

            // Act
            var plugins = definition.Activate(NoServices()).Plugins;

            // Assert
            Assert.Same(editor, Assert.Single(plugins));
        }
    }

    public sealed class Options
    {
        [Fact]
        public void Should_create_fresh_options_for_every_composer()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 7));

            // Act
            var first = definition.Activate(NoServices()).Plugins.OfType<CountingPlugin>().Single().Options;
            var second = definition.Activate(NoServices()).Plugins.OfType<CountingPlugin>().Single().Options;

            // Assert
            Assert.Multiple(
                () => Assert.Equal(7, first.Count),
                () => Assert.Equal(7, second.Count),
                () => Assert.NotSame(first, second));
        }

        [Fact]
        public void Should_leave_a_plugin_its_own_options_when_the_registration_configures_none()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer.AddFeaturePlugin<CountingPlugin>());

            // Act
            var plugin = definition.Activate(NoServices()).Plugins.OfType<CountingPlugin>().Single();

            // Assert
            Assert.Equal(1, plugin.Options.Count);
        }
    }

    public sealed class Defaults
    {
        [Fact]
        public void Should_add_a_default_the_scope_registers_nothing_for()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer.TryAddFeaturePlugin<CountingPlugin>());

            // Act
            var plugins = definition.Activate(NoServices()).Plugins;

            // Assert
            Assert.IsType<CountingPlugin>(Assert.Single(plugins));
        }

        [Fact]
        public void Should_keep_what_the_scope_registered_over_a_default_asked_for_first()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .TryAddFeaturePlugin<CountingPlugin>()
                .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 4));

            // Act
            var plugin = Assert.IsType<CountingPlugin>(Assert.Single(definition.Activate(NoServices()).Plugins));

            // Assert
            Assert.Equal(4, plugin.Options.Count);
        }

        [Fact]
        public void Should_keep_what_the_scope_registered_over_a_default_asked_for_last()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 4)
                .TryAddFeaturePlugin<CountingPlugin>());

            // Act
            var plugin = Assert.IsType<CountingPlugin>(Assert.Single(definition.Activate(NoServices()).Plugins));

            // Assert
            Assert.Equal(4, plugin.Options.Count);
        }

        [Fact]
        public void Should_keep_only_the_last_of_two_explicit_registrations()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 2)
                .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 9));

            // Act
            var plugin = Assert.IsType<CountingPlugin>(Assert.Single(definition.Activate(NoServices()).Plugins));

            // Assert
            Assert.Equal(9, plugin.Options.Count);
        }
    }

    public sealed class Refusals
    {
        [Fact]
        public void Should_name_the_plugin_and_the_parameter_nothing_can_supply()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer.AddFeaturePlugin<NeedyPlugin>());

            // Act
            var refusal = Assert.Throws<InvalidOperationException>(() => definition.Activate(NoServices()));

            // Assert
            Assert.Multiple(
                () => Assert.Contains(nameof(NeedyPlugin), refusal.Message, StringComparison.Ordinal),
                () => Assert.Contains(nameof(Uri), refusal.Message, StringComparison.Ordinal),
                () => Assert.Contains("address", refusal.Message, StringComparison.Ordinal));
        }

        [Fact]
        public void Should_name_a_plugin_offering_more_than_one_constructor()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer.AddFeaturePlugin<AmbiguousPlugin>());

            // Act
            var refusal = Assert.Throws<InvalidOperationException>(() => definition.Activate(NoServices()));

            // Assert
            Assert.Contains(nameof(AmbiguousPlugin), refusal.Message, StringComparison.Ordinal);
        }
    }

    public sealed class Ownership
    {
        [Fact]
        public async Task Should_dispose_a_plugin_it_built_itself()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer.AddFeaturePlugin<ClosablePlugin>());
            var set = definition.Activate(NoServices());
            var plugin = set.Plugins.OfType<ClosablePlugin>().Single();

            // Act
            await set.DisposeAsync();

            // Assert
            Assert.True(plugin.IsDisposed);
        }

        [Fact]
        public async Task Should_leave_a_plugin_a_factory_supplied_to_whoever_wrote_the_factory()
        {
            // Arrange
            using var plugin = new ClosablePlugin();
            var definition = ComposerDefinition.Create(composer => composer.AddFeaturePlugin(_ => plugin));
            var set = definition.Activate(NoServices());

            // Act
            await set.DisposeAsync();

            // Assert
            Assert.False(plugin.IsDisposed);
        }
    }

    public sealed class Description
    {
        [Fact]
        public void Should_drop_what_a_replaced_registration_had_registered_under_it()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>(editor => editor.AddFeaturePlugin<EditorBoundPlugin>())
                .AddEditorPlugin<StubEditor>(editor => editor.AddFeaturePlugin<AlsoEditorBoundPlugin>()));

            // Act
            var names = definition.Activate(NoServices()).Plugins.Select(plugin => plugin.Id).ToList();

            // Assert
            Assert.Equal(["stub-editor", "also-editor-bound"], names);
        }

        [Fact]
        public void Should_order_a_registration_ahead_of_what_is_registered_under_it()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>(editor => editor.AddFeaturePlugin<EditorBoundPlugin>())
                .AddFeaturePlugin<CountingPlugin>());

            // Act
            var names = definition.Activate(NoServices()).Plugins.Select(plugin => plugin.Id).ToList();

            // Assert
            Assert.Equal(["stub-editor", "editor-bound", "counting"], names);
        }

        [Fact]
        public void Should_leave_the_rest_in_place_when_one_registration_is_replaced()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddFeaturePlugin<CountingPlugin>()
                .AddFeaturePlugin<ClosablePlugin>()
                .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 5));

            // Act
            var names = definition.Activate(NoServices()).Plugins.Select(plugin => plugin.Id).ToList();

            // Assert
            Assert.Equal(["counting", "closable"], names);
        }
        [Fact]
        public void Should_build_one_plugin_per_place_it_is_registered()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>(editor => editor
                    .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 3))
                .AddEditorPlugin<SecondEditor>(editor => editor
                    .AddFeaturePlugin<CountingPlugin, CountOptions>(options => options.Count = 8)));

            // Act
            var counting = definition.Activate(NoServices()).Plugins.OfType<CountingPlugin>().ToList();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(2, counting.Count),
                () => Assert.NotSame(counting[0], counting[1]),
                () => Assert.Equal([3, 8], counting.Select(plugin => plugin.Options.Count)));
        }
        [Fact]
        public void Should_activate_nothing_for_an_empty_description()
        {
            // Act
            var plugins = ComposerDefinition.Empty.Activate(NoServices()).Plugins;

            // Assert
            Assert.Empty(plugins);
        }

        [Fact]
        public void Should_keep_an_editor_registered_under_another_editor_apart()
        {
            // Arrange
            var definition = ComposerDefinition.Create(composer => composer
                .AddEditorPlugin<StubEditor>(editor => editor.AddFeaturePlugin<EditorBoundPlugin>())
                .AddEditorPlugin<SecondEditor>());

            // Act
            var plugins = definition.Activate(NoServices()).Plugins;

            // Assert
            Assert.Collection(
                plugins,
                plugin => Assert.IsType<StubEditor>(plugin),
                plugin => Assert.IsType<EditorBoundPlugin>(plugin),
                plugin => Assert.IsType<SecondEditor>(plugin));
        }
    }
}
