namespace Forma.UI.Blazor.Tests.Design;

using System.Linq;

using Forma.UI.Blazor.Design;
using Forma.UI.Design.Icons;

public sealed class UnicodeIconProviderTests
{
    private const char TextPresentation = '\uFE0E';

    // The glyphs whose characters Unicode also defines as emoji, each with a standardized text-presentation sequence.
    private static readonly Glyph[] EmojiCapable =
    [
        Glyph.Airplane, Glyph.Anchor, Glyph.CheckCircle, Glyph.CheckSquare, Glyph.Cut, Glyph.Emoji, Glyph.Error,
        Glyph.Heart, Glyph.Hourglass, Glyph.Information, Glyph.Keyboard, Glyph.Play, Glyph.Settings, Glyph.Warning,
    ];

    [Fact]
    public void Should_request_text_presentation_for_every_emoji_capable_character()
    {
        // Act & Assert
        // Without the selector these arrive as color emoji on platforms that default to emoji presentation.
        Assert.All(EmojiCapable, glyph =>
        {
            var artwork = UnicodeIconProvider.Default.GetArtwork(glyph);

            Assert.EndsWith(TextPresentation.ToString(), Assert.IsType<IconCharacter>(artwork).Text, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Should_not_mark_a_text_only_character_with_the_selector()
    {
        // Arrange
        var textOnly = Enum.GetValues<Glyph>().Where(glyph => glyph is not Glyph.None).Except(EmojiCapable);

        // Act & Assert
        // The selector only means something where Unicode defines a variation sequence; elsewhere it is noise.
        Assert.All(textOnly, glyph =>
        {
            if (UnicodeIconProvider.Default.TryGetArtwork(glyph, out var artwork) && artwork is IconCharacter character)
            {
                Assert.DoesNotContain(TextPresentation, character.Text);
            }
        });
    }

    [Fact]
    public void Should_answer_for_every_drawable_glyph_without_throwing()
    {
        // Arrange
        var provider = UnicodeIconProvider.Default;
        var drawable = Enum.GetValues<Glyph>().Where(glyph => glyph is not Glyph.None);

        // Act & Assert
        // A host that registered no artwork must never be crashed by a glyph this provider cannot depict.
        Assert.All(drawable, glyph => Assert.NotNull(provider.TryGetArtwork(glyph, out var artwork) ? artwork : IconArtwork.None));
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
    public void Should_offer_nothing_for_a_glyph_no_character_depicts()
    {
        // Act
        var found = UnicodeIconProvider.Default.TryGetArtwork(Glyph.AddColumnLeft, out var artwork);

        // Assert
        // A meaningless box in a toolbar reads worse than an empty one.
        Assert.Multiple(
            () => Assert.False(found),
            () => Assert.Same(IconArtwork.None, artwork));
    }

    [Fact]
    public void Should_refuse_to_get_artwork_no_character_depicts()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeIconProvider.Default.GetArtwork(Glyph.AddColumnLeft));
    }

    [Fact]
    public void Should_draw_a_glyph_the_same_character_at_either_variant()
    {
        // Act
        var primary = UnicodeIconProvider.Default.GetArtwork(new Icon(Glyph.Heart));
        var alternate = UnicodeIconProvider.Default.GetArtwork(Icon.Alternate(Glyph.Heart));

        // Assert
        // Both variants carry one character; the renderer draws the alternate in the heavier cut.
        Assert.Equal(primary, alternate);
    }

    [Fact]
    public void Should_refuse_the_absent_glyph()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeIconProvider.Default.GetArtwork(default));
    }
}
