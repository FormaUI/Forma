namespace Forma.Framework.Css.Tests;

using Forma.Framework.Css;

using Xunit;

public sealed class CssStyleBuilderTests
{
    [Fact]
    public void Should_render_the_declarations_added_in_order_each_terminated()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        builder.Add("left", "40px").Add("top", "30px");

        // Assert
        Assert.Equal("left: 40px; top: 30px;", builder.ToString());
    }

    [Fact]
    public void Should_add_nothing_for_a_blank_value()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        builder.Add("left", (string?)null).Add("top", string.Empty).Add("width", "   ");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(builder.IsEmpty);
            Assert.Equal(string.Empty, builder.ToString());
        });
    }

    [Fact]
    public void Should_refuse_a_blank_property()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        var exception = Assert.Throws<ArgumentException>(() => builder.Add(" ", "40px"));

        // Assert
        Assert.Equal("property", exception.ParamName);
    }

    [Fact]
    public void Should_add_a_declaration_only_while_its_condition_holds()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        builder.Add("left", "40px", condition: false).Add("top", "30px", condition: true);

        // Assert
        Assert.Equal("top: 30px;", builder.ToString());
    }

    [Fact]
    public void Should_render_a_length_in_its_unit_regardless_of_culture()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        builder.Add("inline-size", CssLength.FromRem(18.5)).Add("block-size", (CssLength?)null);

        // Assert
        Assert.Equal("inline-size: 18.5rem;", builder.ToString());
    }

    [Fact]
    public void Should_terminate_declarations_given_as_text_once()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        builder.Add("inline-size", "12ch").Add("color: red").Add(" margin: 0; ").Add("  ").Add(null);

        // Assert
        Assert.Equal("inline-size: 12ch; color: red; margin: 0;", builder.ToString());
    }

    [Fact]
    public void Should_be_empty_again_after_clearing()
    {
        // Arrange
        var builder = new CssStyleBuilder();
        builder.Add("left", "40px");

        // Act
        builder.Clear();

        // Assert
        Assert.True(builder.IsEmpty);
    }
}
