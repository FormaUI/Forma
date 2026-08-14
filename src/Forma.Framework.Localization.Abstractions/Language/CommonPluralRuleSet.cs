namespace Forma.Framework.Localization.Language;

/// <summary>The cardinal plural rules from CLDR, covering every locale it publishes.</summary>
public sealed partial class CommonPluralRuleSet : IPluralRuleSet
{
    /// <summary>A shared instance, since the rules carry no state.</summary>
    public static IPluralRuleSet Default { get; } = new CommonPluralRuleSet();

    /// <summary>The category the operands fall into, or null when CLDR carries no rules for the locale.</summary>
    public PluralCategory? Select(string locale, PluralOperands operands)
    {
        ArgumentNullException.ThrowIfNull(locale);

        // CLDR keys a handful of entries by script or region, so the fullest identifier has to win over its language.
        var candidate = locale;
        while (candidate.Length != 0)
        {
            if (SelectExact(candidate, operands) is { } category)
            {
                return category;
            }

            var subtag = candidate.LastIndexOf('-', StringComparison.Ordinal);
            if (subtag < 0)
            {
                return null;
            }

            candidate = candidate[..subtag];
        }

        return null;
    }

    // A CLDR range matches integers only, so 3.5 falls outside 3..10.
    private static bool InRange(decimal value, int low, int high)
        => decimal.Truncate(value) == value && value >= low && value <= high;
}
