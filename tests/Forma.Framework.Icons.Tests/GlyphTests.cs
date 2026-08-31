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
}
