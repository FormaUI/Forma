namespace Forma.Framework.Localization;

/// <summary>The cardinal plural rules transcribed from CLDR 48, for the languages Forma ships with.</summary>
public sealed class PluralRuleSet : IPluralRuleSet
{
    /// <summary>A shared instance, since the rules carry no state.</summary>
    public static IPluralRuleSet Default { get; } = new PluralRuleSet();

    /// <summary>The category the operands fall into, or null when CLDR data for the language is not built in.</summary>
    public PluralCategory? Select(string language, PluralOperands operands)
    {
        var n = operands.Absolute;
        var i = operands.IntegerPart;
        var v = operands.FractionDigits;

        return language switch
        {
            // one: i = 1 and v = 0
            "en" => i == 1 && v == 0 ? PluralCategory.One : PluralCategory.Other,

            // one: n = 1                     many: i != 0 and i % 1000000 = 0 and v = 0
            "es" => n == 1 ? PluralCategory.One
                : Millions(i, v) ? PluralCategory.Many
                : PluralCategory.Other,

            // one: i = 1 and v = 0           many: i != 0 and i % 1000000 = 0 and v = 0
            "it" => i == 1 && v == 0 ? PluralCategory.One
                : Millions(i, v) ? PluralCategory.Many
                : PluralCategory.Other,

            // one: i = 0,1                   many: i != 0 and i % 1000000 = 0 and v = 0
            "fr" => i is 0 or 1 ? PluralCategory.One
                : Millions(i, v) ? PluralCategory.Many
                : PluralCategory.Other,

            // one: i = 1 and v = 0 or i = 0 and v != 0       two: i = 2 and v = 0
            "he" => (i == 1 && v == 0) || (i == 0 && v != 0) ? PluralCategory.One
                : i == 2 && v == 0 ? PluralCategory.Two
                : PluralCategory.Other,

            // zero: n = 0   one: n = 1   two: n = 2   few: n % 100 = 3..10   many: n % 100 = 11..99
            "ar" => n == 0 ? PluralCategory.Zero
                : n == 1 ? PluralCategory.One
                : n == 2 ? PluralCategory.Two
                : InRange(n % 100, 3, 10) ? PluralCategory.Few
                : InRange(n % 100, 11, 99) ? PluralCategory.Many
                : PluralCategory.Other,

            _ => null,
        };
    }

    private static bool Millions(long integerPart, int fractionDigits)
        => integerPart != 0 && integerPart % 1000000 == 0 && fractionDigits == 0;

    // A CLDR range matches integers only, so 3.5 falls outside 3..10.
    private static bool InRange(decimal value, int low, int high)
        => decimal.Truncate(value) == value && value >= low && value <= high;
}
