namespace Forma.UI.Design.Icons.Tests;

using Forma.UI.Design.Icons;

using Xunit;

public sealed class IconTests
{
    [Fact]
    public void Should_default_to_no_glyph_in_the_default_variant()
    {
        // Act
        var icon = default(Icon);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Glyph.None, icon.Glyph),
            () => Assert.Equal(GlyphVariant.Default, icon.Variant));
    }

    [Fact]
    public void Should_take_a_glyph_in_its_default_variant()
    {
        // Act
        Icon icon = Glyph.Delete;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Glyph.Delete, icon.Glyph),
            () => Assert.Equal(GlyphVariant.Default, icon.Variant));
    }

    [Fact]
    public void Should_take_a_glyph_in_its_alternate_variant()
    {
        // Act
        var icon = Icon.Alternate(Glyph.Heart);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Glyph.Heart, icon.Glyph),
            () => Assert.Equal(GlyphVariant.Alternate, icon.Variant));
    }

    [Fact]
    public void Should_tell_two_variants_of_one_glyph_apart()
    {
        // Act
        var primary = new Icon(Glyph.Heart);
        var alternate = Icon.Alternate(Glyph.Heart);

        // Assert
        Assert.NotEqual(primary, alternate);
    }

    [Fact]
    public void Should_treat_the_same_glyph_and_variant_as_equal()
    {
        // Act
        Icon converted = Glyph.Save;
        var constructed = new Icon(Glyph.Save, GlyphVariant.Default);

        // Assert
        Assert.Equal(converted, constructed);
    }
}
