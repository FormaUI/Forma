namespace Forma.Framework.Localization.Language;

/// <summary>The CLDR operands a plural rule is written against.</summary>
public readonly record struct PluralOperands
{
    private PluralOperands(
        decimal absoluteValue,
        long integerPart,
        int fractionDigits,
        int significantFractionDigits,
        long fractionValue,
        long significantFractionValue)
    {
        AbsoluteValue = absoluteValue;
        IntegerPart = integerPart;
        FractionDigits = fractionDigits;
        SignificantFractionDigits = significantFractionDigits;
        FractionValue = fractionValue;
        SignificantFractionValue = significantFractionValue;
    }

    /// <summary>CLDR n — the absolute value of the number.</summary>
    public decimal AbsoluteValue { get; }

    /// <summary>CLDR i — the integer part.</summary>
    public long IntegerPart { get; }

    /// <summary>CLDR v — the count of visible fraction digits, trailing zeros included.</summary>
    public int FractionDigits { get; }

    /// <summary>CLDR w — the count of visible fraction digits, trailing zeros excluded.</summary>
    public int SignificantFractionDigits { get; }

    /// <summary>CLDR f — the visible fraction digits read as an integer, trailing zeros included.</summary>
    public long FractionValue { get; }

    /// <summary>CLDR t — the visible fraction digits read as an integer, trailing zeros excluded.</summary>
    public long SignificantFractionValue { get; }

    /// <summary>CLDR e — the compact decimal exponent, which is zero for a number written out in full.</summary>
    public int Exponent { get; }

    /// <summary>The operands of a number.</summary>
    /// <remarks>The visible fraction digits are the decimal's scale, so <c>1.50m</c> has two.</remarks>
    public static PluralOperands From(decimal value)
    {
        var absoluteValue = Math.Abs(value);

        var scale = (decimal.GetBits(absoluteValue)[3] >> 16) & 0xFF;

        var truncatedValue = decimal.Truncate(absoluteValue);
        var integerPart = truncatedValue > long.MaxValue ? long.MaxValue : (long)truncatedValue;

        var fractionValue = GetFractionDigits(absoluteValue - truncatedValue, scale);

        var significantValue = fractionValue;
        var significantDigits = scale;
        while (significantValue != 0 && significantValue % 10 == 0)
        {
            significantValue /= 10;
            significantDigits--;
        }

        return new PluralOperands(
            absoluteValue,
            integerPart,
            scale,
            significantValue == 0 ? 0 : significantDigits,
            fractionValue,
            significantValue);
    }

    // A count carrying more visible digits than a long holds keeps its low-order ones, which is all a rule reads.
    private static long GetFractionDigits(decimal fraction, int scale)
    {
        if (fraction == 0m)
        {
            return 0L;
        }

        var shiftedFraction = fraction;
        for (var movedDigitCount = 0; movedDigitCount < scale; movedDigitCount++)
        {
            shiftedFraction *= 10m;
        }

        shiftedFraction = decimal.Truncate(shiftedFraction);

        return (long)(shiftedFraction > long.MaxValue ? shiftedFraction % 1000000000000000000m : shiftedFraction);
    }
}
