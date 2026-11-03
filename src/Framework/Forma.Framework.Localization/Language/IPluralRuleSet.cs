namespace Forma.Framework.Localization.Language;

using System.Globalization;

/// <summary>Decides which plural category a count falls into for a locale.</summary>
public interface IPluralRuleSet
{
    /// <summary>The category the operands fall into for a locale, null for a locale without rules.</summary>
    PluralCategory? Select(string locale, PluralOperands operands);

    /// <summary>The category a number falls into for a culture.</summary>
    PluralCategory Select(CultureInfo culture, decimal value)
    {
        ArgumentNullException.ThrowIfNull(culture);

        return Select(culture.Name, PluralOperands.From(value)) ?? PluralCategory.Other;
    }
}
