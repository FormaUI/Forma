namespace Forma.UI.Features.Icons.Phosphor.Tests;

using System.Collections.Generic;
using System.Linq;

using Forma.UI.Design.Icons;

using Xunit;

public sealed class PhosphorIconProviderTests
{
    private static IEnumerable<Glyph> Drawable
        => Enum.GetValues<Glyph>().Where(glyph => glyph is not Glyph.None);

    [Fact]
    public void Should_carry_artwork_for_every_glyph_in_every_style()
    {
        // Arrange
        var provider = new PhosphorIconProvider();

        // Act & Assert
        foreach (var glyph in Drawable)
        {
            foreach (var style in Enum.GetValues<GlyphStyle>())
            {
                var vector = Assert.IsType<IconVector>(provider.GetArtwork(new Icon(glyph, style)));

                Assert.Multiple(
                    () => Assert.Equal("0 0 256 256", vector.ViewBox),
                    () => Assert.NotEmpty(vector.Paths),
                    () => Assert.All(vector.Paths, path => Assert.False(string.IsNullOrWhiteSpace(path.Data))));
            }
        }
    }

    [Fact]
    public void Should_refuse_the_absent_glyph()
    {
        // Arrange
        var provider = new PhosphorIconProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetArtwork(default));
    }

    [Fact]
    public void Should_draw_a_filled_glyph_differently_from_its_regular()
    {
        // Arrange
        var provider = new PhosphorIconProvider();

        // Act
        var regular = Assert.IsType<IconVector>(provider.GetArtwork(new Icon(Glyph.Heart)));
        var filled = Assert.IsType<IconVector>(provider.GetArtwork(Icon.Filled(Glyph.Heart)));

        // Assert
        Assert.NotEqual(regular.Paths[0].Data, filled.Paths[0].Data);
    }

    [Fact]
    public void Should_draw_every_path_fully_opaque_by_default()
    {
        // Arrange
        var provider = new PhosphorIconProvider();

        // Act
        var artwork = Assert.IsType<IconVector>(provider.GetArtwork(new Icon(Glyph.Delete)));

        // Assert
        Assert.All(artwork.Paths, path => Assert.Equal(1, path.Opacity));
    }
}
