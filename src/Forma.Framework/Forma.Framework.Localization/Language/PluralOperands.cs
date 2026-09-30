namespace Forma.Framework.Localization.Language;

/// <summary>The CLDR operands a plural rule is written against.</summary>
public readonly record struct PluralOperands
{
    private PluralOperands(
        decimal absolute,
        long integerPart,
        int fractionDigits,
        int significantFractionDigits,
        long fractionValue,
        long significantFractionValue)
    {
        Absolute = absolute;
        IntegerPart = integerPart;
        FractionDigits = fractionDigits;
        SignificantFractionDigits = significantFractionDigits;
        FractionValue = fractionValue;
        SignificantFractionValue = significantFractionValue;
    }

    /// <summary>CLDR n — the absolute value of the number.</summary>
    public decimal Absolute { get; }

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
        var absolute = Math.Abs(value);

        var scale = (decimal.GetBits(absolute)[3] >> 16) & 0xFF;

        var truncated = decimal.Truncate(absolute);
        var integerPart = truncated > long.MaxValue ? long.MaxValue : (long)truncated;

        var fractionValue = GetFractionDigits(absolute - truncated, scale);

        var significantValue = fractionValue;
        var significantDigits = scale;
        while (significantValue != 0 && significantValue % 10 == 0)
        {
            significantValue /= 10;
            significantDigits--;
        }

        return new PluralOperands(
            absolute,
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

        var shifted = fraction;
        for (var moved = 0; moved < scale; moved++)
        {
            shifted *= 10m;
        }

        shifted = decimal.Truncate(shifted);

        return (long)(shifted > long.MaxValue ? shifted % 1000000000000000000m : shifted);
    }
}
