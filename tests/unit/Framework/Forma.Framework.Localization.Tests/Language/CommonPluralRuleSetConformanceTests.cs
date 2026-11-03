namespace Forma.Framework.Localization.Tests.Language;

public sealed class CommonPluralRuleSetConformanceTests
{
    [Fact]
    public void Should_match_every_sample_CLDR_publishes()
    {
        // Arrange
        var failures = new List<string>();

        // Act
        foreach (var (locale, count, expected) in PluralRuleSamples.All)
        {
            var value = decimal.Parse(count, CultureInfo.InvariantCulture);
            var selected = CommonPluralRuleSet.Default.Select(locale, PluralOperands.From(value)) ?? PluralCategory.Other;

            if (selected != expected)
            {
                failures.Add($"{locale} {count}: expected {expected}, selected {selected}");
            }
        }

        // Assert
        Assert.True(failures.Count == 0, $"CLDR {PluralRuleSamples.CldrVersion}:\n{string.Join("\n", failures)}");
    }

    [Fact]
    public void Should_prefer_a_regional_rule_over_its_language()
    {
        // Arrange — Portuguese counts zero as singular, except in Portugal.
        var operands = PluralOperands.From(0m);

        // Act
        var brazil = CommonPluralRuleSet.Default.Select("pt-BR", operands);
        var portugal = CommonPluralRuleSet.Default.Select("pt-PT", operands);

        // Assert
        Assert.Equal(PluralCategory.One, brazil);
        Assert.Equal(PluralCategory.Other, portugal);
    }

    [Fact]
    public void Should_fall_back_from_a_region_to_its_language()
    {
        // Act — CLDR carries no es-CO, so Spanish answers for it.
        var category = CommonPluralRuleSet.Default.Select("es-CO", PluralOperands.From(1000000m));

        // Assert
        Assert.Equal(PluralCategory.Many, category);
    }

    [Fact]
    public void Should_report_a_locale_it_carries_no_rules_for()
        => Assert.Null(CommonPluralRuleSet.Default.Select("zxx-Zzzz", PluralOperands.From(2m)));
}
