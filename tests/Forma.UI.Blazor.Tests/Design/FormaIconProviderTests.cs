namespace Forma.UI.Blazor.Tests.Design;

using Forma.UI.Blazor.Design;
using Forma.UI.Design.Icons;

public sealed class FormaIconProviderTests
{
    private static IEnumerable<Glyph> Drawable
        => Enum.GetValues<Glyph>().Where(glyph => glyph is not Glyph.None);

    [Fact]
    public void Should_carry_artwork_for_every_glyph_in_every_variant_at_every_size()
    {
        // Act & Assert
        foreach (var glyph in Drawable)
        {
            foreach (var variant in Enum.GetValues<GlyphVariant>())
            {
                foreach (var size in Enum.GetValues<IconSize>())
                {
                    var artwork = FormaIconProvider.Default.GetArtwork(new Icon(glyph, variant), size);

                    Assert.NotEmpty(Assert.IsType<IconVector>(artwork).Paths);
                }
            }
        }
    }

    [Fact]
    public void Should_draw_a_stroke_glyph_with_its_stroke_width()
    {
        // Act
        var artwork = Assert.IsType<IconVector>(FormaIconProvider.Default.GetArtwork(Glyph.Check, IconSize.Inline));

        // Assert
        Assert.All(artwork.Paths, path => Assert.Equal(2, path.StrokeWidth));
    }

    [Fact]
    public void Should_draw_a_filled_alternate_without_a_stroke()
    {
        // Act
        var artwork = Assert.IsType<IconVector>(FormaIconProvider.Default.GetArtwork(Icon.Alternate(Glyph.Heart), IconSize.Inline));

        // Assert
        Assert.All(artwork.Paths, path => Assert.Equal(0, path.StrokeWidth));
    }

    [Fact]
    public void Should_fill_a_solid_dot_inside_a_stroke_glyph()
    {
        // Act
        var artwork = Assert.IsType<IconVector>(FormaIconProvider.Default.GetArtwork(Glyph.CircleInfo, IconSize.Inline));

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
        var primary = Assert.IsType<IconVector>(FormaIconProvider.Default.GetArtwork(Glyph.ArrowRight, IconSize.Inline));
        var alternate = Assert.IsType<IconVector>(FormaIconProvider.Default.GetArtwork(Icon.Alternate(Glyph.ArrowRight), IconSize.Inline));

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
        var artwork = Assert.IsType<IconVector>(FormaIconProvider.Default.GetArtwork(Glyph.Check, size));

        // Assert
        Assert.Equal(viewBox, artwork.ViewBox);
    }

    [Fact]
    public void Should_refuse_the_absent_glyph()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => FormaIconProvider.Default.GetArtwork(default, IconSize.Inline));
    }
}
