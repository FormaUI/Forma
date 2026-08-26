namespace Forma.UI.Blazor.Tests.Design;

using System.Linq;

using Forma.UI.Blazor.Design;
using Forma.UI.Design.Icons;

public sealed class UnicodeIconProviderTests
{
    [Fact]
    public void Should_answer_for_every_drawable_glyph_without_throwing()
    {
        // Arrange
        var provider = UnicodeIconProvider.Default;
        var drawable = Enum.GetValues<Glyph>().Where(glyph => glyph is not Glyph.None);

        // Act & Assert
        // A host that registered no artwork must never be crashed by a glyph this provider cannot depict.
        Assert.All(drawable, glyph => Assert.NotNull(provider.GetArtwork(glyph)));
    }

    [Fact]
    public void Should_draw_a_mapped_glyph_as_its_character()
    {
        // Act
        var artwork = UnicodeIconProvider.Default.GetArtwork(Glyph.Check);

        // Assert
        Assert.Equal("✓", Assert.IsType<IconCharacter>(artwork).Text);
    }

    [Fact]
    public void Should_draw_nothing_for_a_glyph_no_character_depicts()
    {
        // Act
        var artwork = UnicodeIconProvider.Default.GetArtwork(Glyph.AddColumnLeft);

        // Assert
        // A meaningless box in a toolbar reads worse than an empty one.
        Assert.Same(IconArtwork.None, artwork);
    }

    [Fact]
    public void Should_draw_a_glyph_the_same_at_either_style()
    {
        // Act
        var regular = UnicodeIconProvider.Default.GetArtwork(new Icon(Glyph.Heart));
        var filled = UnicodeIconProvider.Default.GetArtwork(Icon.Filled(Glyph.Heart));

        // Assert
        // A character carries no weight of its own, so asking for the filled style cannot change it.
        Assert.Equal(regular, filled);
    }

    [Fact]
    public void Should_refuse_the_absent_glyph()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeIconProvider.Default.GetArtwork(default));
    }
}
