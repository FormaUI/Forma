namespace Forma.Framework.Icons.Tests;

using Forma.Framework.Icons;

using Xunit;

public sealed class GlyphTests
{
    [Fact]
    public void Should_reserve_zero_for_no_glyph()
        => Assert.Equal(0, (int)Glyph.None);

    [Fact]
    public void Should_reserve_zero_of_the_variant_for_default()
        => Assert.Equal(0, (int)GlyphVariant.Default);

    [Theory]
    [InlineData(Glyph.ArrowLeft, true)]
    [InlineData(Glyph.ChevronRight, true)]
    [InlineData(Glyph.Undo, true)]
    [InlineData(Glyph.Indent, true)]
    [InlineData(Glyph.ListBullet, true)]
    [InlineData(Glyph.AlignLeft, false)]
    [InlineData(Glyph.TriangleRight, false)]
    [InlineData(Glyph.Search, false)]
    [InlineData(Glyph.Refresh, false)]
    [InlineData(Glyph.Check, false)]
    public void Should_adapt_a_glyph_pointing_along_the_reading_direction_for_right_to_left(Glyph glyph, bool expected)
        => Assert.Equal(expected, glyph.IsDirectionSensitive("rtl"));

    [Fact]
    public void Should_name_the_direction_a_glyph_adapts_for()
        => Assert.Multiple(
            () => Assert.Equal("rtl", Glyph.ArrowLeft.GetDirectionSensitivity()),
            () => Assert.Null(Glyph.Check.GetDirectionSensitivity()));

    [Fact]
    public void Should_read_the_direction_as_the_browser_does_whatever_its_case()
        => Assert.True(Glyph.ArrowLeft.IsDirectionSensitive("RTL"));

    [Fact]
    public void Should_adapt_both_of_a_left_and_right_pair_or_neither()
    {
        // Arrange
        var pairs = Enum.GetValues<Glyph>()
            .Where(glyph => glyph.ToString().EndsWith("Left", StringComparison.Ordinal))
            .Select(left => (Left: left, Right: Enum.Parse<Glyph>(left.ToString()[..^"Left".Length] + "Right")))
            .ToArray();

        // Assert
        Assert.NotEmpty(pairs);
        Assert.All(pairs, pair => Assert.Equal(pair.Left.GetDirectionSensitivity(), pair.Right.GetDirectionSensitivity()));
    }

    [Theory]
    [InlineData(Glyph.Undo, Glyph.Redo)]
    [InlineData(Glyph.Reply, Glyph.Forward)]
    [InlineData(Glyph.Indent, Glyph.Outdent)]
    [InlineData(Glyph.Exit, Glyph.SignIn)]
    public void Should_adapt_both_of_a_pair_of_opposites(Glyph one, Glyph other)
        => Assert.Multiple(
            () => Assert.True(one.IsDirectionSensitive("rtl")),
            () => Assert.True(other.IsDirectionSensitive("rtl")));
}
