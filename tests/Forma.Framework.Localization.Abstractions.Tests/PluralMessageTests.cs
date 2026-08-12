namespace Forma.Framework.Localization.Abstractions.Tests;

public sealed class PluralMessageTests
{
    private const string AllBranches = "{0, plural, zero {none} one {# one} two {# two} few {# few} many {# many} other {# other}}";

    [Theory]
    [InlineData(PluralCategory.Zero, "none")]
    [InlineData(PluralCategory.One, "3 one")]
    [InlineData(PluralCategory.Two, "3 two")]
    [InlineData(PluralCategory.Few, "3 few")]
    [InlineData(PluralCategory.Many, "3 many")]
    [InlineData(PluralCategory.Other, "3 other")]
    public void Should_choose_the_branch_the_rules_select(PluralCategory category, string expected)
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(category));

        // Act
        var expanded = resolver.Expand(AllBranches, CultureInfo.InvariantCulture, [3]);

        // Assert
        Assert.Equal(expected, expanded);
    }

    [Fact]
    public void Should_fall_back_to_other_when_the_translation_omits_the_chosen_branch()
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(PluralCategory.Many));

        // Act
        var expanded = resolver.Expand("{0, plural, one {# file} other {# files}}", CultureInfo.InvariantCulture, [7]);

        // Assert
        Assert.Equal("7 files", expanded);
    }

    [Fact]
    public void Should_fall_back_to_other_when_the_rules_carry_no_entry_for_the_language()
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(category: null));

        // Act
        var expanded = resolver.Expand(AllBranches, CultureInfo.InvariantCulture, [3]);

        // Assert
        Assert.Equal("3 other", expanded);
    }

    [Fact]
    public void Should_leave_other_holes_open_for_formatting()
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var expanded = resolver.Expand("{0, plural, other {# files}} in {1}", CultureInfo.InvariantCulture, [2, "Photos"]);

        // Assert
        Assert.Equal("2 files in {1}", expanded);
    }

    [Fact]
    public void Should_resolve_every_plural_construct_in_one_sentence()
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var expanded = resolver.Expand(
            "{0, plural, one {# file} other {# files}} in {1, plural, one {# folder} other {# folders}}",
            CultureInfo.InvariantCulture,
            [1, 1]);

        // Assert
        Assert.Equal("1 file in 1 folder", expanded);
    }

    [Fact]
    public void Should_return_a_format_without_plurals_unchanged()
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var expanded = resolver.Expand("Stored {0} of {1}.", CultureInfo.InvariantCulture, [1, 2]);

        // Assert
        Assert.Equal("Stored {0} of {1}.", expanded);
    }

    [Fact]
    public void Should_treat_a_missing_argument_as_no_count()
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(PluralCategory.Zero));

        // Act
        var expanded = resolver.Expand(AllBranches, CultureInfo.InvariantCulture, []);

        // Assert
        Assert.Equal("none", expanded);
    }

    [Theory]
    [InlineData("{0, plural,")]
    [InlineData("{0, plural, one {")]
    [InlineData("{, plural, one {x} other {y}}")]
    [InlineData(", plural, one {x}")]
    public void Should_leave_a_malformed_plural_construct_alone(string format)
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var expanded = resolver.Expand(format, CultureInfo.InvariantCulture, [1]);

        // Assert
        Assert.Equal(format, expanded);
    }

    [Fact]
    public void Should_treat_a_count_that_is_not_a_number_as_zero()
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var expanded = resolver.Expand("{0, plural, other {# files}}", CultureInfo.InvariantCulture, ["abc"]);

        // Assert
        Assert.Equal("0 files", expanded);
    }

    [Fact]
    public void Should_reject_a_null_rule_set()
        => Assert.Throws<ArgumentNullException>(() => new PluralMessage(null!));
    [Fact]
    public void Should_reject_a_null_format()
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(PluralCategory.Other));

        // Assert
        Assert.Throws<ArgumentNullException>(() => resolver.Expand(null!, CultureInfo.InvariantCulture, []));
    }

    [Fact]
    public void Should_reject_a_null_culture()
    {
        // Arrange
        var resolver = new PluralMessage(new StubPluralRuleSet(PluralCategory.Other));

        // Assert
        Assert.Throws<ArgumentNullException>(() => resolver.Expand(AllBranches, null!, []));
    }
}
