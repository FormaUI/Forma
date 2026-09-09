namespace Forma.Framework.Css.Tests;

using Forma.Framework.Css;

using Xunit;

public sealed class CssClassBuilderTests
{
    [Fact]
    public void Should_render_the_classes_added_in_order()
    {
        // Arrange
        var builder = new CssClassBuilder();

        // Act
        builder.Add("forma-frame").Add("is-placed").Add("host-pane");

        // Assert
        Assert.Equal("forma-frame is-placed host-pane", builder.ToString());
    }

    [Fact]
    public void Should_add_nothing_for_a_blank_class()
    {
        // Arrange
        var builder = new CssClassBuilder();

        // Act
        builder.Add(null).Add(string.Empty).Add("   ");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(builder.IsEmpty);
            Assert.Equal(string.Empty, builder.ToString());
        });
    }

    [Fact]
    public void Should_add_a_class_only_while_its_condition_holds()
    {
        // Arrange
        var builder = new CssClassBuilder();

        // Act
        builder.Add("is-maximized", condition: true).Add("is-locked", condition: false);

        // Assert
        Assert.Equal("is-maximized", builder.ToString());
    }

    [Fact]
    public void Should_be_empty_again_once_cleared()
    {
        // Arrange
        var builder = new CssClassBuilder();
        builder.Add("forma-frame");

        // Act
        builder.Clear();

        // Assert
        Assert.True(builder.IsEmpty);
    }
}
