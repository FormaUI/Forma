namespace Forma.Framework.Icons.Glyphs.Tests;

using System.Text;

using Forma.Framework.Icons;

using Xunit;

public sealed class GlyphIconProviderTests
{
    private static readonly GlyphIconProvider Provider = new();

    private static IEnumerable<Glyph> DrawableGlyphs
        => Enum.GetValues<Glyph>().Where(glyph => glyph is not Glyph.None);

    private static IEnumerable<Brand> DrawableBrands
        => Enum.GetValues<Brand>().Where(brand => brand is not Brand.None);

    [Fact]
    public void Should_carry_artwork_for_every_glyph_in_every_variant_at_every_size()
    {
        // Act & Assert
        foreach (var glyph in DrawableGlyphs)
        {
            foreach (var variant in Enum.GetValues<GlyphVariant>())
            {
                foreach (var size in Enum.GetValues<IconSize>())
                {
                    var artwork = Provider.GetArtwork(Icon.FromGlyph(glyph, variant), size);

                    Assert.NotEmpty(Assert.IsType<IconVector>(artwork, exactMatch: false).Paths);
                }
            }
        }
    }

    [Fact]
    public void Should_draw_a_stroke_glyph_with_its_stroke_width()
    {
        // Act
        var artwork = Assert.IsType<IconVector>(Provider.GetArtwork(Glyph.Check, IconSize.Inline));

        // Assert
        Assert.All(artwork.Paths, path => Assert.Equal(2, path.StrokeWidth));
    }

    [Fact]
    public void Should_draw_a_filled_alternate_without_a_stroke()
    {
        // Act
        var artwork = Assert.IsType<IconVector>(Provider.GetArtwork(Icon.FromGlyph(Glyph.Heart, GlyphVariant.Alternate), IconSize.Inline));

        // Assert
        Assert.All(artwork.Paths, path => Assert.Equal(0, path.StrokeWidth));
    }

    [Fact]
    public void Should_fill_a_solid_dot_inside_a_stroke_glyph()
    {
        // Act
        var artwork = Assert.IsType<IconVector>(Provider.GetArtwork(Glyph.CircleInfo, IconSize.Inline));

        // Assert
        // The stem is stroked; the dot above it is a solid shape a stroke cannot draw.
        Assert.Multiple(
            () => Assert.Contains(artwork.Paths, path => path.StrokeWidth > 0),
            () => Assert.Contains(artwork.Paths, path => path.StrokeWidth == 0));
    }

    [Fact]
    public void Should_draw_the_alternate_of_a_glyph_with_nothing_to_fill_heavier()
    {
        // Act
        var primary = Assert.IsType<IconVector>(Provider.GetArtwork(Glyph.ArrowRight, IconSize.Inline));
        var alternate = Assert.IsType<IconVector>(Provider.GetArtwork(Icon.FromGlyph(Glyph.ArrowRight, GlyphVariant.Alternate), IconSize.Inline));

        // Assert
        Assert.Multiple(
            () => Assert.All(primary.Paths, path => Assert.Equal(2, path.StrokeWidth)),
            () => Assert.All(alternate.Paths, path => Assert.Equal(3, path.StrokeWidth)));
    }

    [Theory]
    [InlineData(IconSize.Inline, "0 0 32 32")]
    [InlineData(IconSize.Small, "0 0 24 24")]
    [InlineData(IconSize.Medium, "0 0 32 32")]
    [InlineData(IconSize.Large, "0 0 24 24")]
    [InlineData(IconSize.Display, "0 0 32 32")]
    public void Should_draw_the_grid_whose_stroke_lands_on_pixels_at_the_size(IconSize size, string viewBox)
    {
        // Act
        var artwork = Assert.IsType<IconVector>(Provider.GetArtwork(Glyph.Check, size));

        // Assert
        Assert.Equal(viewBox, artwork.ViewBox);
    }

    [Fact]
    public void Should_refuse_the_absent_glyph()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Provider.GetArtwork(Glyph.None, IconSize.Inline));

    [Fact]
    public void Should_carry_a_titled_mark_for_every_brand_at_every_size()
    {
        // Act & Assert
        foreach (var brand in DrawableBrands)
        {
            foreach (var size in Enum.GetValues<IconSize>())
            {
                var artwork = Assert.IsType<BrandIconArtwork>(Provider.GetArtwork(brand, size));

                Assert.Multiple(
                    () => Assert.Single(artwork.Paths),
                    () => Assert.False(string.IsNullOrWhiteSpace(artwork.Title), $"{brand} carries no title."));
            }
        }
    }

    [Fact]
    public void Should_draw_a_brand_mark_as_one_solid_path()
    {
        // Act
        var artwork = Assert.IsType<BrandIconArtwork>(Provider.GetArtwork(Brand.GitHub, IconSize.Inline));

        // Assert
        Assert.Multiple(
            () => Assert.Equal("GitHub", artwork.Title),
            () => Assert.Equal(0, Assert.Single(artwork.Paths).StrokeWidth));
    }

    [Fact]
    public void Should_answer_no_artwork_for_a_brand_with_no_mark_compiled()
    {
        // Arrange
        // A member the enum will never define, standing in for a brand whose mark has not been compiled.
        var unknown = (Brand)int.MaxValue;

        // Act
        var found = Provider.TryGetArtwork(unknown, IconSize.Inline, out var artwork);

        // Assert
        Assert.Multiple(
            () => Assert.False(found),
            () => Assert.Same(IconArtwork.None, artwork));
    }

    [Fact]
    public void Should_refuse_the_absent_brand()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Provider.GetArtwork(Brand.None, IconSize.Inline));

    [Fact]
    public void Should_answer_any_character_with_itself()
    {
        // Act
        var artwork = Provider.GetArtwork(new Rune('Ω'), IconSize.Small);

        // Assert
        Assert.Equal(new Rune('Ω'), Assert.IsType<IconCharacter>(artwork).Value);
    }
}
