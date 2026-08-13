namespace Forma.Framework.Localization.Cldr;

using System.Globalization;

/// <summary>Decides which plural category a count falls into for a language.</summary>
public interface IPluralRuleSet
{
    /// <summary>The category the operands fall into, or null when the language has no rule here.</summary>
    PluralCategory? Select(string language, PluralOperands operands);

    /// <summary>The category a number falls into for a culture, falling back to <see cref="PluralCategory.Other"/>.</summary>
    PluralCategory Select(CultureInfo culture, decimal value)
    {
        ArgumentNullException.ThrowIfNull(culture);

        return Select(culture.TwoLetterISOLanguageName, PluralOperands.From(value)) ?? PluralCategory.Other;
    }
}
