namespace Forma.Framework.Localization.Tests.Language;

using Forma.Framework.Localization.Language;

public sealed class PluralExpressionEvaluatorTests
{
    private const string AllFields = "[$ [0] plural; zero: none; one: # one; two: # two; few: # few; many: # many; other: # other; $]";

    [Theory]
    [InlineData(PluralCategory.Zero, "none")]
    [InlineData(PluralCategory.One, "3 one")]
    [InlineData(PluralCategory.Two, "3 two")]
    [InlineData(PluralCategory.Few, "3 few")]
    [InlineData(PluralCategory.Many, "3 many")]
    [InlineData(PluralCategory.Other, "3 other")]
    public void Should_choose_the_field_the_rules_select(PluralCategory category, string expected)
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(category));

        // Act
        var evaluated = evaluator.Evaluate(AllFields, CultureInfo.InvariantCulture, [3]);

        // Assert
        Assert.Equal(expected, evaluated);
    }

    [Fact]
    public void Should_fall_back_to_other_when_the_translation_omits_the_chosen_field()
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Many));

        // Act
        var evaluated = evaluator.Evaluate("[$ [0] plural; one: # file; other: # files; $]", CultureInfo.InvariantCulture, [7]);

        // Assert
        Assert.Equal("7 files", evaluated);
    }

    [Fact]
    public void Should_fall_back_to_other_when_the_rules_carry_no_entry_for_the_language()
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(category: null));

        // Act
        var evaluated = evaluator.Evaluate(AllFields, CultureInfo.InvariantCulture, [3]);

        // Assert
        Assert.Equal("3 other", evaluated);
    }

    [Fact]
    public void Should_leave_other_holes_open_for_formatting()
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var evaluated = evaluator.Evaluate("[$ [0] plural; other: # files; $] in {1}", CultureInfo.InvariantCulture, [2, "Photos"]);

        // Assert
        Assert.Equal("2 files in {1}", evaluated);
    }

    [Fact]
    public void Should_resolve_every_plural_expression_in_one_sentence()
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(
            "[$ [0] plural; one: # file; other: # files; $] in [$ [1] plural; one: # folder; other: # folders; $]",
            CultureInfo.InvariantCulture,
            [1, 1]);

        // Assert
        Assert.Equal("1 file in 1 folder", evaluated);
    }

    [Fact]
    public void Should_return_a_format_without_plurals_unchanged()
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var evaluated = evaluator.Evaluate("Stored {0} of {1}.", CultureInfo.InvariantCulture, [1, 2]);

        // Assert
        Assert.Equal("Stored {0} of {1}.", evaluated);
    }

    [Fact]
    public void Should_treat_a_missing_argument_as_no_count()
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Zero));

        // Act
        var evaluated = evaluator.Evaluate(AllFields, CultureInfo.InvariantCulture, []);

        // Assert
        Assert.Equal("none", evaluated);
    }

    [Theory]
    [InlineData("[$ [0] plural; one: # file")]
    [InlineData("[$ [0] plural; one: # file; other: # files $]")]
    [InlineData("[$ {0}; one: x; other: y; $]")]
    [InlineData("[$ plural; one: x; other: y; $]")]
    public void Should_leave_a_malformed_plural_expression_alone(string format)
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(format, CultureInfo.InvariantCulture, [1]);

        // Assert
        Assert.Equal(format, evaluated);
    }

    // Only a plural expression belongs to this evaluator, and dropping the rest would destroy them.
    [Fact]
    public void Should_leave_a_expression_named_for_something_else_alone()
    {
        // Arrange
        const string Format = "[$ select {0}; male: his; female: her; $] bike";
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(Format, CultureInfo.InvariantCulture, [1]);

        // Assert
        Assert.Equal(Format, evaluated);
    }

    [Fact]
    public void Should_treat_a_count_that_is_not_a_number_as_zero()
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var evaluated = evaluator.Evaluate("[$ [0] plural; other: # files; $]", CultureInfo.InvariantCulture, ["abc"]);

        // Assert
        Assert.Equal("0 files", evaluated);
    }

    [Fact]
    public void Should_reject_a_null_rule_set()
        => Assert.Throws<ArgumentNullException>(() => new PluralExpressionEvaluator(null!));
    [Fact]
    public void Should_reject_a_null_format()
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Assert
        Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate(null!, CultureInfo.InvariantCulture, []));
    }

    [Fact]
    public void Should_reject_a_null_culture()
    {
        // Arrange
        var evaluator = new PluralExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Assert
        Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate(AllFields, null!, []));
    }
}
