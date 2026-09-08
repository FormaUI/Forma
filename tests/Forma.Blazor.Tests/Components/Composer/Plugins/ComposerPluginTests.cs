namespace Forma.Blazor.Tests.Components.Composer.Plugins;

using Forma.Blazor.Components.Composer.Plugins;
using Forma.Framework.Composer.Plugins;

public sealed class ComposerPluginTests
{
    public sealed class Maximize
    {
        [Fact]
        public void Should_apply_to_every_editor()
        {
            // Arrange
            IComposerFeaturePlugin plugin = new MaximizePlugin();

            // Assert
            Assert.Equal("maximize", plugin.Id);
            Assert.Empty(plugin.SupportedEditorIds);
            Assert.NotEmpty(plugin.Items);
        }
    }

    public sealed class SelectionBar
    {
        [Fact]
        public void Should_apply_to_every_editor()
        {
            // Arrange
            IComposerFeaturePlugin plugin = new SelectionBarPlugin();

            // Assert
            Assert.Equal("selection-bar", plugin.Id);
            Assert.Empty(plugin.SupportedEditorIds);
            Assert.NotEmpty(plugin.Items);
        }
    }
}
