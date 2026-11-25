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
        builder.Add("left", CssLength.FromPx(40)).Add("top", CssLength.FromPx(30));

        // Assert
        Assert.Equal("left: 40px; top: 30px;", builder.ToString());
    }

    [Fact]
    public void Should_add_nothing_for_a_missing_value()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        builder.Add("left", (CssLength?)null).Add("color", (CssColorValue?)null);

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
        var exception = Assert.Throws<ArgumentException>(() => builder.Add(" ", CssLength.FromPx(40)));

        // Assert
        Assert.Equal("property", exception.ParamName);
    }

    [Fact]
    public void Should_add_a_declaration_only_while_its_condition_holds()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        builder.Add("left", CssLength.FromPx(40), condition: false).Add("top", CssLength.FromPx(30), condition: true);

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
    public void Should_render_a_color_literally_or_as_the_property_a_theme_defines_it_in()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        builder.Add("color", CssColor.FromHex(0x2c384a)).Add("background-color", CssColorValue.FromProperty("--success-subtle-color"));

        // Assert
        Assert.Equal("color: #2c384a; background-color: var(--success-subtle-color);", builder.ToString());
    }

    [Fact]
    public void Should_terminate_declarations_given_as_text_once()
    {
        // Arrange
        var builder = new CssStyleBuilder();

        // Act
        builder.Add("inline-size", CssLength.FromCh(12)).AddDeclarations("color: red").AddDeclarations(" margin: 0; ").AddDeclarations("  ").AddDeclarations(null);

        // Assert
        Assert.Equal("inline-size: 12ch; color: red; margin: 0;", builder.ToString());
    }

    [Fact]
    public void Should_answer_null_instead_of_an_empty_value()
    {
        // Arrange
        var empty = new CssStyleBuilder();
        var filled = new CssStyleBuilder().Add("left", CssLength.FromPx(40));

        // Act
        var nothing = empty.ToStringOrNull();
        var value = filled.ToStringOrNull();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Null(nothing);
            Assert.Equal("left: 40px;", value);
        });
    }

    [Fact]
    public void Should_be_empty_again_after_clearing()
    {
        // Arrange
        var builder = new CssStyleBuilder();
        builder.Add("left", CssLength.FromPx(40));

        // Act
        builder.Clear();

        // Assert
        Assert.True(builder.IsEmpty);
    }
}
