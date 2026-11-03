namespace Forma.Tests.Localization;

using Forma.Framework.Localization.Language;

/// <summary>A plural rule set that selects one category for every locale.</summary>
/// <param name="category">The category, where null reports the locale as unknown.</param>
public sealed class StubPluralRuleSet(PluralCategory? category) : IPluralRuleSet
{
    /// <inheritdoc />
    public PluralCategory? Select(string locale, PluralOperands operands)
        => category;
}
