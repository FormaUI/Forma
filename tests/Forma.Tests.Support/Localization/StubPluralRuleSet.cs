namespace Forma.Tests.Localization;

using Forma.Framework.Localization.Language;

/// <summary>A rule set returning the category a test dictates, regardless of language.</summary>
/// <param name="category">The category to return, or null to report the language as unknown.</param>
public sealed class StubPluralRuleSet(PluralCategory? category) : IPluralRuleSet
{
    /// <summary>The dictated category.</summary>
    public PluralCategory? Select(string language, PluralOperands operands)
        => category;
}
