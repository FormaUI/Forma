namespace Forma.Framework.Css.Tests;

using Forma.Framework.Css;

using Xunit;

public sealed class CssColorValueTests
{
    [Fact]
    public void Should_render_a_literal_color_as_the_color_renders()
    {
        // Act
        var value = CssColorValue.FromColor(CssColor.FromRgba(98, 105, 118, 0.12));

        // Assert
        Assert.Equal("rgba(98, 105, 118, 0.12)", value.ToString());
    }

    [Fact]
    public void Should_take_a_literal_color_where_one_is_given()
    {
        // Act
        CssColorValue value = CssColor.FromHex(0x2c384a);

        // Assert
        Assert.Equal(CssColorValue.FromColor(CssColor.FromHex(0x2c384a)), value);
    }

    [Fact]
    public void Should_render_a_custom_property_as_its_reference()
    {
        // Act
        var value = CssColorValue.FromProperty("--success-subtle-color");

        // Assert
        Assert.Equal("var(--success-subtle-color)", value.ToString());
    }

    [Fact]
    public void Should_equal_another_value_reading_the_same_custom_property()
    {
        // Act
        var first = CssColorValue.FromProperty("--success-color");
        var second = CssColorValue.FromProperty("--success-color");

        // Assert
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("success-color")]
    [InlineData("-success-color")]
    [InlineData("--success color")]
    [InlineData("--success;color")]
    [InlineData("var(--success-color)")]
    public void Should_refuse_a_name_that_isn_t_a_custom_property_s(string propertyName)
    {
        // Act
        var exception = Record.Exception(() => CssColorValue.FromProperty(propertyName));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_refuse_a_blank_name(string propertyName)
    {
        // Act
        var exception = Record.Exception(() => CssColorValue.FromProperty(propertyName));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }
}
