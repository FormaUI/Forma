namespace Forma.Framework.Icons.Tests;

using System.Text;

using Forma.Framework.Icons;

using Xunit;

public sealed class IconTests
{
    [Fact]
    public void Should_take_a_glyph_in_its_default_variant()
    {
        // Act
        Icon icon = Glyph.Delete;

        // Assert
        var glyph = Assert.IsType<GlyphIcon>(icon);
        Assert.Multiple(
            () => Assert.Equal(Glyph.Delete, glyph.Glyph),
            () => Assert.Equal(GlyphVariant.Default, glyph.Variant));
    }

    [Fact]
    public void Should_take_a_glyph_in_a_variant()
    {
        // Act
        var icon = Icon.FromGlyph(Glyph.Heart, GlyphVariant.Alternate);

        // Assert
        var glyph = Assert.IsType<GlyphIcon>(icon);
        Assert.Multiple(
            () => Assert.Equal(Glyph.Heart, glyph.Glyph),
            () => Assert.Equal(GlyphVariant.Alternate, glyph.Variant));
    }

    [Fact]
    public void Should_take_a_brand()
    {
        // Act
        Icon icon = Brand.GitHub;

        // Assert
        Assert.Equal(Brand.GitHub, Assert.IsType<BrandIcon>(icon).Brand);
    }

    [Fact]
    public void Should_take_a_character()
    {
        // Act
        Icon icon = new Rune('Ω');

        // Assert
        Assert.Equal(new Rune('Ω'), Assert.IsType<CharacterIcon>(icon).Character);
    }

    [Fact]
    public void Should_tell_two_variants_of_one_glyph_apart()
        => Assert.NotEqual(Icon.FromGlyph(Glyph.Heart), Icon.FromGlyph(Glyph.Heart, GlyphVariant.Alternate));

    [Fact]
    public void Should_treat_the_same_glyph_and_variant_as_equal()
    {
        // Act
        Icon converted = Glyph.Save;
        var constructed = Icon.FromGlyph(Glyph.Save, GlyphVariant.Default);

        // Assert
        Assert.Equal(converted, constructed);
    }

    [Fact]
    public void Should_tell_a_glyph_from_a_brand_from_a_character()
    {
        // Arrange
        Icon[] icons = [Glyph.Star, Brand.X, new Rune('X')];

        // Assert
        Assert.Equal(3, icons.Distinct().Count());
    }
}
