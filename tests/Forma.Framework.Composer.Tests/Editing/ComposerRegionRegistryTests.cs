namespace Forma.Framework.Composer.Tests.Editing;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Icons;

using Xunit;

public sealed class ComposerRegionRegistryTests
{
    [Fact]
    public void Should_list_a_status_item_once_created()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();

        // Act
        registry.CreateStatusItem("Ln 1, Col 1");

        // Assert
        Assert.Equal(["Ln 1, Col 1"], registry.StatusItems.Select(i => i.Text));
    }

    [Fact]
    public void Should_remove_a_status_item_when_disposed()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();
        var item = registry.CreateStatusItem("Ln 1, Col 1");

        // Act
        item.Dispose();

        // Assert
        Assert.Empty(registry.StatusItems);
    }

    [Fact]
    public void Should_reflect_a_status_items_text_after_it_is_updated()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();
        var item = registry.CreateStatusItem("Ln 1, Col 1");

        // Act
        item.Text = "Ln 2, Col 4";

        // Assert
        Assert.Equal("Ln 2, Col 4", registry.StatusItems[0].Text);
    }

    [Fact]
    public void Should_leave_a_status_item_unannounced_by_default()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();

        // Act
        var item = registry.CreateStatusItem("Ln 1, Col 1");

        // Assert
        Assert.False(item.IsAnnounced);
    }

    [Fact]
    public void Should_raise_changed_when_a_status_item_opts_into_being_announced()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();
        var item = registry.CreateStatusItem("Saved");
        var raised = false;
        registry.Changed += () => raised = true;

        // Act
        item.IsAnnounced = true;

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void Should_raise_changed_when_a_status_item_is_created()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();
        var raised = false;
        registry.Changed += () => raised = true;

        // Act
        registry.CreateStatusItem("Ln 1, Col 1");

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void Should_raise_changed_when_a_status_item_is_updated()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();
        var item = registry.CreateStatusItem("Ln 1, Col 1");
        var raiseCount = 0;
        registry.Changed += () => raiseCount++;

        // Act
        item.Text = "Ln 2, Col 1";

        // Assert
        Assert.Equal(1, raiseCount);
    }

    [Fact]
    public void Should_raise_changed_when_a_status_item_is_disposed()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();
        var item = registry.CreateStatusItem("Ln 1, Col 1");
        var raiseCount = 0;
        registry.Changed += () => raiseCount++;

        // Act
        item.Dispose();

        // Assert
        Assert.Equal(1, raiseCount);
    }

    [Fact]
    public void Should_not_raise_changed_again_when_a_status_item_is_disposed_twice()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();
        var item = registry.CreateStatusItem("Ln 1, Col 1");
        item.Dispose();
        var raiseCount = 0;
        registry.Changed += () => raiseCount++;

        // Act
        item.Dispose();

        // Assert
        Assert.Equal(0, raiseCount);
    }

    [Fact]
    public void Should_list_a_gutter_marker_once_created()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();

        // Act
        registry.CreateGutterMarker(3, Glyph.RadioButton);

        // Assert
        var marker = Assert.Single(registry.GutterMarkers);
        Assert.Multiple(
            () => Assert.Equal(3, marker.Block),
            () => Assert.Equal(Glyph.RadioButton, marker.Icon));
    }

    [Fact]
    public void Should_remove_a_gutter_marker_when_disposed()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();
        var marker = registry.CreateGutterMarker(3, Glyph.RadioButton);

        // Act
        marker.Dispose();

        // Assert
        Assert.Empty(registry.GutterMarkers);
    }

    [Fact]
    public void Should_reject_a_negative_block()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => registry.CreateGutterMarker(-1, Glyph.RadioButton));
    }

    [Fact]
    public void Should_keep_status_items_in_creation_order()
    {
        // Arrange
        var registry = new ComposerRegionRegistry();

        // Act
        registry.CreateStatusItem("First");
        registry.CreateStatusItem("Second");

        // Assert
        Assert.Equal(["First", "Second"], registry.StatusItems.Select(i => i.Text));
    }
}
