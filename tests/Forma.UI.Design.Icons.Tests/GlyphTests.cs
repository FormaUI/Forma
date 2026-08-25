namespace Forma.UI.Design.Icons.Tests;

using Forma.UI.Design.Icons;

using Xunit;

public sealed class GlyphTests
{
    [Fact]
    public void Should_reserve_zero_for_no_glyph()
        => Assert.Equal(0, (int)Glyph.None);

    [Fact]
    public void Should_reserve_zero_of_the_style_for_regular()
        => Assert.Equal(0, (int)GlyphStyle.Regular);
}
