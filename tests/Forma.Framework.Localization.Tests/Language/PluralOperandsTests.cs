namespace Forma.Framework.Localization.Tests.Language;

public sealed class PluralOperandsTests
{
    [Theory]
    [InlineData("1", 1, 0)]
    [InlineData("1.0", 1, 1)]
    [InlineData("1.00", 1, 2)]
    [InlineData("0.5", 0, 1)]
    [InlineData("1000000", 1000000, 0)]
    public void Should_read_the_integer_part_and_visible_digits(string number, long integerPart, int fractionDigits)
    {
        // Act
        var operands = PluralOperands.From(decimal.Parse(number, CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(integerPart, operands.IntegerPart);
        Assert.Equal(fractionDigits, operands.FractionDigits);
    }

    [Fact]
    public void Should_take_a_negative_count_by_its_magnitude()
    {
        // Act
        var operands = PluralOperands.From(-2.5m);

        // Assert
        Assert.Equal(2.5m, operands.Absolute);
        Assert.Equal(2, operands.IntegerPart);
    }

    [Fact]
    public void Should_clamp_an_integer_part_wider_than_a_long()
    {
        // Act
        var operands = PluralOperands.From(decimal.MaxValue);

        // Assert
        Assert.Equal(long.MaxValue, operands.IntegerPart);
    }

    [Fact]
    public void Should_clamp_the_most_negative_value_without_overflowing()
    {
        // Act
        var operands = PluralOperands.From(decimal.MinValue);

        // Assert
        Assert.Equal(long.MaxValue, operands.IntegerPart);
    }

    [Theory]
    [InlineData("1", 0, 0, 0, 0)]
    [InlineData("1.0", 1, 0, 0, 0)]
    [InlineData("1.00", 2, 0, 0, 0)]
    [InlineData("0.5", 1, 1, 5, 5)]
    [InlineData("1.230", 3, 2, 230, 23)]
    [InlineData("2.0300", 4, 2, 300, 3)]
    public void Should_read_the_fraction_digits_as_a_value(string number, int digits, int significantDigits, long value, long significantValue)
    {
        // Act
        var operands = PluralOperands.From(decimal.Parse(number, CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(digits, operands.FractionDigits);
        Assert.Equal(significantDigits, operands.SignificantFractionDigits);
        Assert.Equal(value, operands.FractionValue);
        Assert.Equal(significantValue, operands.SignificantFractionValue);
    }

    [Fact]
    public void Should_report_no_exponent_for_a_number_written_out_in_full()
        => Assert.Equal(0, PluralOperands.From(1000000m).Exponent);
}
