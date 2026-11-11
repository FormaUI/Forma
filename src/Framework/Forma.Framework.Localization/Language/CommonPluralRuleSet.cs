namespace Forma.Framework.Localization.Language;

/// <summary>The cardinal plural rules from CLDR, covering every locale it publishes.</summary>
public sealed partial class CommonPluralRuleSet : IPluralRuleSet
{
    /// <summary>The shared instance.</summary>
    public static IPluralRuleSet Default { get; } = new CommonPluralRuleSet();

    /// <inheritdoc />
    public PluralCategory? Select(string locale, PluralOperands operands)
    {
        ArgumentNullException.ThrowIfNull(locale);

        // CLDR keys a few entries by script or region, so the fullest identifier is tried before its language.
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
    private static bool InRange(decimal value, int lowBound, int highBound)
        => decimal.Truncate(value) == value && value >= lowBound && value <= highBound;
}
