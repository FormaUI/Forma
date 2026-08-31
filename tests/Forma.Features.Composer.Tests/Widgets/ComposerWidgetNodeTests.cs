namespace Forma.Features.Composer.Tests.Widgets;

using System.Text.Json;

using Forma.Features.Composer.Widgets;

using Xunit;

public sealed class ComposerWidgetNodeTests
{
    [Fact]
    public void Should_clone_settings_so_they_outlive_the_source_document()
    {
        // Arrange
        ComposerWidgetNode node;

        // Act
        using (var json = JsonDocument.Parse("""{"width":50}"""))
        {
            node = new ComposerWidgetNode
            {
                InstanceId = "n1",
                WidgetId = "column",
                Settings = json.RootElement
            };
        }

        // Assert - the parse is disposed, so an unowned element would throw here
        Assert.Equal(50, node.Settings.GetProperty("width").GetInt32());
    }

    [Fact]
    public void Should_accept_a_node_carrying_no_settings()
    {
        // Act
        var node = new ComposerWidgetNode { InstanceId = "n1", WidgetId = "spacer" };

        // Assert
        Assert.Multiple(
            () => Assert.Equal(JsonValueKind.Undefined, node.Settings.ValueKind),
            () => Assert.Empty(node.Children));
    }
}
