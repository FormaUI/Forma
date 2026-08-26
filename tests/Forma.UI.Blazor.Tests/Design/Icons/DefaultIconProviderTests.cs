namespace Forma.UI.Blazor.Tests.Design.Icons;

using System.Collections.Generic;
using System.Linq;

using Forma.UI.Blazor.Design.Icons;
using Forma.UI.Design.Icons;

public sealed class DefaultIconProviderTests
{
    private static IEnumerable<Glyph> Drawable
        => Enum.GetValues<Glyph>().Where(glyph => glyph is not Glyph.None);

    [Fact]
    public void Should_carry_artwork_for_every_glyph_in_every_style()
    {
        // Arrange
        var provider = new DefaultIconProvider();

        // Act & Assert
        foreach (var glyph in Drawable)
        {
            foreach (var style in Enum.GetValues<GlyphStyle>())
            {
                var artwork = provider.GetArtwork(new Icon(glyph, style));

                Assert.Multiple(
                    () => Assert.Equal("0 0 256 256", artwork.ViewBox),
                    () => Assert.NotEmpty(artwork.Paths),
                    () => Assert.All(artwork.Paths, path => Assert.False(string.IsNullOrWhiteSpace(path.Data))));
            }
        }
    }

    [Fact]
    public void Should_refuse_the_absent_glyph()
    {
        // Arrange
        var provider = new DefaultIconProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetArtwork(default));
    }

    [Fact]
    public void Should_draw_a_filled_glyph_differently_from_its_regular()
    {
        // Arrange
        var provider = new DefaultIconProvider();

        // Act
        var regular = provider.GetArtwork(new Icon(Glyph.Heart));
        var filled = provider.GetArtwork(Icon.Filled(Glyph.Heart));

        // Assert
        Assert.NotEqual(regular.Paths[0].Data, filled.Paths[0].Data);
    }

    [Fact]
    public void Should_draw_every_path_fully_opaque_by_default()
    {
        // Arrange
        var provider = new DefaultIconProvider();

        // Act
        var artwork = provider.GetArtwork(new Icon(Glyph.Delete));

        // Assert
        Assert.All(artwork.Paths, path => Assert.Equal(1, path.Opacity));
    }
}
