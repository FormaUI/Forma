namespace Forma.Framework.Localization.Language;

/// <summary>The CLDR operands a plural rule is written against.</summary>
public readonly record struct PluralOperands
{
    private PluralOperands(decimal absolute, long integerPart, int fractionDigits)
    {
        Absolute = absolute;
        IntegerPart = integerPart;
        FractionDigits = fractionDigits;
    }

    /// <summary>CLDR n — the absolute value of the number.</summary>
    public decimal Absolute { get; }

    /// <summary>CLDR i — the integer part.</summary>
    public long IntegerPart { get; }

    /// <summary>CLDR v — the count of visible fraction digits, trailing zeros included.</summary>
    public int FractionDigits { get; }

    /// <summary>Derives the operands from a number, reading the visible digit count from its scale.</summary>
    public static PluralOperands From(decimal value)
    {
        var absolute = Math.Abs(value);

        // decimal keeps the authored scale, which is exactly CLDR's visible fraction digit count.
        var scale = (decimal.GetBits(absolute)[3] >> 16) & 0xFF;

        var truncated = decimal.Truncate(absolute);
        var integerPart = truncated > long.MaxValue ? long.MaxValue : (long)truncated;

        return new PluralOperands(absolute, integerPart, scale);
    }
}
