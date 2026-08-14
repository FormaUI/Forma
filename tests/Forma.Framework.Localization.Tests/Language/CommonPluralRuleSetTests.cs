namespace Forma.Framework.Localization.Tests.Language;

public sealed class CommonPluralRuleSetTests
{
    [Theory]
    // English distinguishes only one, and a visible fraction leaves it.
    [InlineData("en", "0", PluralCategory.Other)]
    [InlineData("en", "1", PluralCategory.One)]
    [InlineData("en", "2", PluralCategory.Other)]
    [InlineData("en", "1.0", PluralCategory.Other)]
    [InlineData("en", "1000000", PluralCategory.Other)]

    // Spanish matches on the value rather than the integer part, so 1.0 stays singular.
    [InlineData("es", "0", PluralCategory.Other)]
    [InlineData("es", "1", PluralCategory.One)]
    [InlineData("es", "1.0", PluralCategory.One)]
    [InlineData("es", "1000000", PluralCategory.Many)]

    // Italian matches on the integer part, so 1.0 is plural, and millions take their own form.
    [InlineData("it", "1", PluralCategory.One)]
    [InlineData("it", "1.0", PluralCategory.Other)]
    [InlineData("it", "1000000", PluralCategory.Many)]

    // French counts zero as singular.
    [InlineData("fr", "0", PluralCategory.One)]
    [InlineData("fr", "1", PluralCategory.One)]
    [InlineData("fr", "0.5", PluralCategory.One)]
    [InlineData("fr", "2", PluralCategory.Other)]
    [InlineData("fr", "1000000", PluralCategory.Many)]

    // Hebrew has a dual form, and no longer has a category for multiples of ten.
    [InlineData("he", "0", PluralCategory.Other)]
    [InlineData("he", "1", PluralCategory.One)]
    [InlineData("he", "2", PluralCategory.Two)]
    [InlineData("he", "0.5", PluralCategory.One)]
    [InlineData("he", "20", PluralCategory.Other)]

    // Arabic uses every category.
    [InlineData("ar", "0", PluralCategory.Zero)]
    [InlineData("ar", "1", PluralCategory.One)]
    [InlineData("ar", "2", PluralCategory.Two)]
    [InlineData("ar", "3", PluralCategory.Few)]
    [InlineData("ar", "10", PluralCategory.Few)]
    [InlineData("ar", "11", PluralCategory.Many)]
    [InlineData("ar", "99", PluralCategory.Many)]
    [InlineData("ar", "100", PluralCategory.Other)]
    [InlineData("ar", "3.5", PluralCategory.Other)]
    public void Should_select_the_category_the_language_defines(string language, string number, PluralCategory expected)
    {
        // Arrange
        var value = decimal.Parse(number, CultureInfo.InvariantCulture);

        // Act
        var category = CommonPluralRuleSet.Default.Select(language, PluralOperands.From(value));

        // Assert
        Assert.Equal(expected, category);
    }

    [Fact]
    public void Should_report_a_language_it_carries_no_rules_for()
    {
        // Act
        var category = CommonPluralRuleSet.Default.Select("zxx", PluralOperands.From(2m));

        // Assert
        Assert.Null(category);
    }

    [Fact]
    public void Should_fall_back_to_other_for_a_language_it_carries_no_rules_for()
    {
        // Act
        var category = CommonPluralRuleSet.Default.Select(CultureInfo.GetCultureInfo("zxx"), 2m);

        // Assert
        Assert.Equal(PluralCategory.Other, category);
    }

    [Fact]
    public void Should_read_the_language_from_a_culture()
    {
        // Act
        var category = CommonPluralRuleSet.Default.Select(CultureInfo.GetCultureInfo("he-IL"), 2m);

        // Assert
        Assert.Equal(PluralCategory.Two, category);
    }

    [Fact]
    public void Should_treat_a_negative_count_by_its_magnitude()
    {
        // Act
        var category = CommonPluralRuleSet.Default.Select("ar", PluralOperands.From(-2m));

        // Assert
        Assert.Equal(PluralCategory.Two, category);
    }

    [Fact]
    public void Should_reject_a_null_culture()
        => Assert.Throws<ArgumentNullException>(() => CommonPluralRuleSet.Default.Select(culture: null!, 1m));
}
