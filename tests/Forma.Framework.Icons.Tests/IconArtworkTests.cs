namespace Forma.Framework.Icons.Tests;

using System.Text;

using Forma.Framework.Icons;

using Xunit;

public sealed class IconArtworkTests
{
    [Fact]
    public void Should_draw_nothing_for_none()
        => Assert.IsNotType<IconVector>(IconArtwork.None);

    [Fact]
    public void Should_hold_the_geometry_of_a_vector()
    {
        // Act
        var artwork = IconArtwork.Vector("0 0 32 32", [new IconPath { Data = "M 0 0 L 32 32", StrokeWidth = 2 }]);

        // Assert
        var vector = Assert.IsType<IconVector>(artwork);
        Assert.Multiple(
            () => Assert.Equal("0 0 32 32", vector.ViewBox),
            () => Assert.Single(vector.Paths));
    }

    [Theory]
    [InlineData('Ω')]
    [InlineData('א')]
    public void Should_hold_a_character_from_the_basic_plane(char value)
    {
        // Act
        var artwork = IconArtwork.Character(new Rune(value));

        // Assert
        Assert.Equal(new Rune(value), Assert.IsType<IconCharacter>(artwork).Value);
    }

    [Fact]
    public void Should_hold_a_character_beyond_the_basic_plane()
    {
        // Arrange
        var doubleStruckA = new Rune(0x1D538);

        // Act
        var artwork = IconArtwork.Character(doubleStruckA);

        // Assert
        Assert.Equal("𝔸", Assert.IsType<IconCharacter>(artwork).Value.ToString());
    }

    [Fact]
    public void Should_refuse_half_a_character()
        // A lone surrogate is not a character, and Rune refuses it before any artwork can hold it.
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Rune('\uD835'));
}
