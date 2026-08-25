namespace Forma.UI.Style.Icons.Tests;

using Forma.UI.Style.Icons;

using Xunit;

public sealed class IconTests
{
    [Fact]
    public void Should_default_to_no_glyph_drawn_regular()
    {
        // Act
        var icon = default(Icon);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Glyph.None, icon.Glyph),
            () => Assert.Equal(GlyphStyle.Regular, icon.Style));
    }

    [Fact]
    public void Should_take_a_glyph_at_its_default_style()
    {
        // Act
        Icon icon = Glyph.Delete;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Glyph.Delete, icon.Glyph),
            () => Assert.Equal(GlyphStyle.Regular, icon.Style));
    }

    [Fact]
    public void Should_take_a_glyph_drawn_as_a_solid_shape()
    {
        // Act
        var icon = Icon.Filled(Glyph.Heart);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Glyph.Heart, icon.Glyph),
            () => Assert.Equal(GlyphStyle.Filled, icon.Style));
    }

    [Fact]
    public void Should_tell_two_styles_of_one_glyph_apart()
    {
        // Act
        var regular = new Icon(Glyph.Heart);
        var filled = Icon.Filled(Glyph.Heart);

        // Assert
        Assert.NotEqual(regular, filled);
    }

    [Fact]
    public void Should_treat_the_same_glyph_and_style_as_equal()
    {
        // Act
        Icon converted = Glyph.Save;
        var constructed = new Icon(Glyph.Save, GlyphStyle.Regular);

        // Assert
        Assert.Equal(converted, constructed);
    }
}
