namespace Forma.Blazor.Composer.Tests.Plugins;

using Forma.Blazor.Components.Composer.Plugins;
using Forma.Framework.Composer.Plugins;

public sealed class ComposerPluginTests
{
    public sealed class Maximize
    {
        [Fact]
        public void Should_contribute_its_command()
        {
            // Arrange
            IComposerFeaturePlugin plugin = new MaximizePlugin();

            // Assert
            Assert.Equal("maximize", plugin.Id);
            Assert.NotEmpty(plugin.Items);
        }
    }

    public sealed class Shortcuts
    {
        [Fact]
        public void Should_contribute_its_command_and_a_shortcut_of_its_own()
        {
            // Arrange
            IComposerFeaturePlugin plugin = new ShortcutsPlugin();

            // Assert
            Assert.Equal("shortcuts", plugin.Id);
            Assert.NotEmpty(plugin.Items);
            Assert.Equal("Ctrl+/", Assert.Single(plugin.KeyBindings).Display);
        }
    }

    public sealed class SelectionBar
    {
        [Fact]
        public void Should_contribute_its_command()
        {
            // Arrange
            IComposerFeaturePlugin plugin = new SelectionBarPlugin();

            // Assert
            Assert.Equal("selection-bar", plugin.Id);
            Assert.NotEmpty(plugin.Items);
        }
    }
}
