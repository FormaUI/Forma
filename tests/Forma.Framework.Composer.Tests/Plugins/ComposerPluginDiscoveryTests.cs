namespace Forma.Framework.Composer.Tests.Plugins;

using System.Reflection;

using Forma.Framework.Composer.Plugins;
using Forma.Framework.Composer.Tests.Plugins.Discoverables;

using Xunit;

public sealed class ComposerPluginDiscoveryTests
{
    private static readonly Assembly Here = typeof(ComposerPluginDiscoveryTests).Assembly;

    [Fact]
    public void Should_build_every_feature_plugin_where_it_looks()
    {
        // Arrange
        var editor = new StubEditorPlugin();

        // Act
        var plugins = ComposerPluginDiscovery.Discover([editor], Here, ["Plugins.Discoverables"]);

        // Assert
        Assert.Multiple(
            () => Assert.Same(editor, plugins[0]),
            () => Assert.Contains(plugins, plugin => plugin is StampPlugin),
            () => Assert.Contains(plugins, plugin => plugin is HighlightPlugin),
            () => Assert.Contains(plugins, plugin => plugin is BadgePlugin),
            () => Assert.Contains(plugins, plugin => plugin is OutlineViewPlugin view && ReferenceEquals(view.Editor, editor)));
    }

    [Fact]
    public void Should_leave_a_plugin_that_declares_no_role()
    {
        // Act
        var plugins = ComposerPluginDiscovery.Discover([new StubEditorPlugin()], Here, ["Plugins.Discoverables"]);

        // Assert
        // An editor is a surface a host chooses deliberately, and a bare plugin declares nothing to find.
        Assert.Multiple(
            () => Assert.DoesNotContain(plugins, plugin => plugin is LoosePlugin),
            () => Assert.Single(plugins, plugin => plugin is StubEditorPlugin));
    }

    [Fact]
    public void Should_hand_a_plugin_the_editor_it_builds_on()
    {
        // Arrange
        var editor = new StubEditorPlugin();

        // Act
        var plugins = ComposerPluginDiscovery.Discover([editor], Here, ["Plugins.Discoverables"]);

        // Assert
        var highlight = Assert.IsType<HighlightPlugin>(plugins.Single(plugin => plugin is HighlightPlugin));
        Assert.Same(editor, highlight.Editor);
    }

    [Fact]
    public void Should_leave_optional_settings_to_their_defaults()
    {
        // Act
        var plugins = ComposerPluginDiscovery.Discover([new StubEditorPlugin()], Here, ["Plugins.Discoverables"]);

        // Assert
        var badge = Assert.IsType<BadgePlugin>(plugins.Single(plugin => plugin is BadgePlugin));
        Assert.Same(BadgeOptions.Default, badge.Options);
    }

    [Fact]
    public void Should_not_build_a_plugin_already_given()
    {
        // Arrange
        var editor = new StubEditorPlugin();
        var configured = new BadgePlugin(editor, new BadgeOptions());

        // Act
        var plugins = ComposerPluginDiscovery.Discover([editor, configured], Here, ["Plugins.Discoverables"]);

        // Assert
        // A hand-configured plugin stands for its type, so discovery must not shadow it with a default-built one.
        Assert.Same(configured, plugins.Single(plugin => plugin is BadgePlugin));
    }

    [Fact]
    public void Should_report_a_plugin_it_cannot_build()
    {
        // Act & Assert
        // Reported rather than skipped, because a surface promising every plugin must not quietly miss one.
        var refused = Assert.Throws<InvalidOperationException>(
            () => ComposerPluginDiscovery.Discover([], Here, ["Plugins.Unbuildables"]));

        Assert.Contains("OrphanPlugin", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_find_nothing_outside_the_locations_named()
    {
        // Arrange
        var editor = new StubEditorPlugin();

        // Act
        var plugins = ComposerPluginDiscovery.Discover([editor], Here, ["Plugins.Nowhere"]);

        // Assert
        Assert.Same(editor, Assert.Single(plugins));
    }

    [Fact]
    public void Should_read_a_location_spelled_as_a_folder_path()
    {
        // Act
        var plugins = ComposerPluginDiscovery.Discover([new StubEditorPlugin()], Here, ["Plugins/Discoverables"]);

        // Assert
        Assert.Contains(plugins, plugin => plugin is StampPlugin);
    }

    [Fact]
    public void Should_refuse_no_plugins_at_all()
        => Assert.Throws<ArgumentNullException>(() => ComposerPluginDiscovery.Discover(null!, Here));
}
