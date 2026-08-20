namespace Forma.UI.Style.Tests.Css;

using Forma.Tests.Shared;
using Forma.UI.Style.Css;

using Xunit;

public sealed class CssLengthTests
{
    [Fact]
    public void Should_keep_the_value_and_unit_given()
    {
        // Act
        var length = CssLength.FromRem(18.5);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(18.5, length.Value);
            Assert.Equal(CssLengthUnit.Rem, length.Unit);
        });
    }

    [Theory]
    [InlineData(CssLengthUnit.Ch, "12ch")]
    [InlineData(CssLengthUnit.Em, "12em")]
    [InlineData(CssLengthUnit.Percent, "12%")]
    [InlineData(CssLengthUnit.Px, "12px")]
    [InlineData(CssLengthUnit.Rem, "12rem")]
    public void Should_render_each_unit_with_its_own_suffix(CssLengthUnit unit, string expected)
    {
        // Arrange
        var length = unit switch
        {
            CssLengthUnit.Ch => CssLength.FromCh(12),
            CssLengthUnit.Em => CssLength.FromEm(12),
            CssLengthUnit.Percent => CssLength.FromPercent(12),
            CssLengthUnit.Px => CssLength.FromPx(12),
            _ => CssLength.FromRem(12),
        };

        // Assert
        Assert.Equal(expected, length.ToString());
    }

    [Fact]
    public void Should_drop_a_trailing_zero_fraction()
    {
        // Act
        var length = CssLength.FromPx(240);

        // Assert
        Assert.Equal("240px", length.ToString());
    }

    [Fact]
    public void Should_render_a_decimal_point_under_a_comma_culture()
    {
        // Arrange
        using var fixture = new CultureFixture("es-ES");

        // Act
        var length = CssLength.FromRem(18.5);

        // Assert
        Assert.Equal("18.5rem", length.ToString());
    }

    [Fact]
    public void Should_render_a_negative_length()
    {
        // Act
        var length = CssLength.FromPx(-4);

        // Assert
        Assert.Equal("-4px", length.ToString());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Should_refuse_a_value_that_is_not_finite(double value)
    {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => CssLength.FromRem(value));
    }

    [Fact]
    public void Should_treat_the_same_value_and_unit_as_equal()
    {
        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(CssLength.FromRem(4), CssLength.FromRem(4));
            Assert.NotEqual(CssLength.FromRem(4), CssLength.FromPx(4));
        });
    }
}
