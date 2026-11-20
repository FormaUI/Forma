namespace Forma.Framework.Localization.Tests.Language;

using Forma.Framework.Localization.Language;

public sealed class LocaleExpressionEvaluatorTests
{
    private const string AllFields = "[$ [count] plural; zero: none; one: # one; two: # two; few: # few; many: # many; other: # other; $]";

    private static LocalizedArgument Count(object? value)
        => new("count", value);

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
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(category));

        // Act
        var evaluated = evaluator.Evaluate(AllFields, CultureInfo.InvariantCulture, [Count(3)]);

        // Assert
        Assert.Equal(expected, evaluated);
    }

    [Fact]
    public void Should_leave_a_hash_alone_when_the_text_holds_no_expression()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate("Issue #42 in C# is #1.", CultureInfo.InvariantCulture, []);

        // Assert
        Assert.Equal("Issue #42 in C# is #1.", evaluated);
    }

    [Fact]
    public void Should_leave_a_hash_outside_an_expression_alone()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(
            "Issue #42 has [$ [count] plural; one: # note; other: # notes; $] from C#.",
            CultureInfo.InvariantCulture,
            [Count(1)]);

        // Assert
        Assert.Equal("Issue #42 has 1 note from C#.", evaluated);
    }

    [Fact]
    public void Should_write_a_doubled_hash_in_a_field_as_a_literal_hash()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(
            "[$ [count] plural; one: note ##; other: notes ##; $]",
            CultureInfo.InvariantCulture,
            [Count(1)]);

        // Assert
        Assert.Equal("note #", evaluated);
    }

    [Fact]
    public void Should_write_the_count_beside_a_literal_hash()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var evaluated = evaluator.Evaluate(
            "[$ [count] plural; one: # note on ##1; other: # notes on ##1; $]",
            CultureInfo.InvariantCulture,
            [Count(3)]);

        // Assert
        Assert.Equal("3 notes on #1", evaluated);
    }

    [Fact]
    public void Should_keep_a_doubled_hash_outside_an_expression_as_written()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(
            "Tagged ## and [$ [count] plural; one: # note; other: # notes; $].",
            CultureInfo.InvariantCulture,
            [Count(1)]);

        // Assert
        Assert.Equal("Tagged ## and 1 note.", evaluated);
    }

    [Fact]
    public void Should_read_the_sole_argument_for_an_expression_naming_none()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(
            "[$ plural; one: # file; other: # files; $]",
            CultureInfo.InvariantCulture,
            [LocalizedArgument.Unnamed(1)]);

        // Assert
        Assert.Equal("1 file", evaluated);
    }

    [Fact]
    public void Should_reject_an_expression_naming_no_argument_when_more_than_one_is_supplied()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(
            "[$ plural; one: # file; other: # files; $]",
            CultureInfo.InvariantCulture,
            [Count(1), new LocalizedArgument("other", 2)]));
    }

    [Fact]
    public void Should_reject_an_expression_naming_no_argument_when_the_argument_is_named()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(
            "[$ plural; one: # file; other: # files; $]",
            CultureInfo.InvariantCulture,
            [Count(1)]));
    }

    [Fact]
    public void Should_fall_back_to_other_when_the_text_omits_the_chosen_field()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Many));

        // Act
        var evaluated = evaluator.Evaluate("[$ [count] plural; one: # file; other: # files; $]", CultureInfo.InvariantCulture, [Count(7)]);

        // Assert
        Assert.Equal("7 files", evaluated);
    }

    [Fact]
    public void Should_fall_back_to_other_when_the_rules_carry_no_entry_for_the_language()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(category: null));

        // Act
        var evaluated = evaluator.Evaluate(AllFields, CultureInfo.InvariantCulture, [Count(3)]);

        // Assert
        Assert.Equal("3 other", evaluated);
    }

    [Fact]
    public void Should_leave_other_holes_open_for_formatting()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var evaluated = evaluator.Evaluate("[$ [count] plural; other: # files; $] in {album}", CultureInfo.InvariantCulture, [Count(2), new LocalizedArgument("album", "Photos")]);

        // Assert
        Assert.Equal("2 files in {album}", evaluated);
    }

    [Fact]
    public void Should_resolve_every_plural_expression_in_one_sentence()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(
            "[$ [files] plural; one: # file; other: # files; $] in [$ [folders] plural; one: # folder; other: # folders; $]",
            CultureInfo.InvariantCulture,
            [new LocalizedArgument("files", 1), new LocalizedArgument("folders", 1)]);

        // Assert
        Assert.Equal("1 file in 1 folder", evaluated);
    }

    [Fact]
    public void Should_return_a_format_without_plurals_unchanged()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var evaluated = evaluator.Evaluate("Stored {stored} of {total}.", CultureInfo.InvariantCulture, [new LocalizedArgument("stored", 1), new LocalizedArgument("total", 2)]);

        // Assert
        Assert.Equal("Stored {stored} of {total}.", evaluated);
    }

    [Fact]
    public void Should_treat_a_missing_argument_as_no_count()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Zero));

        // Act
        var evaluated = evaluator.Evaluate(AllFields, CultureInfo.InvariantCulture, []);

        // Assert
        Assert.Equal("none", evaluated);
    }

    [Theory]
    [InlineData("[$ [count] plural; one: # file")]
    [InlineData("[$ [count] plural; one: # file; other: # files $]")]
    [InlineData("[$ {count}; one: x; other: y; $]")]
    public void Should_leave_a_malformed_plural_expression_alone(string format)
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(format, CultureInfo.InvariantCulture, [Count(1)]);

        // Assert
        Assert.Equal(format, evaluated);
    }

    [Fact]
    public void Should_leave_a_expression_named_for_something_else_alone()
    {
        // Arrange
        const string Format = "[$ select {count}; male: his; female: her; $] bike";
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.One));

        // Act
        var evaluated = evaluator.Evaluate(Format, CultureInfo.InvariantCulture, [Count(1)]);

        // Assert
        Assert.Equal(Format, evaluated);
    }

    [Fact]
    public void Should_treat_a_count_that_is_not_a_number_as_zero()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Act
        var evaluated = evaluator.Evaluate("[$ [count] plural; other: # files; $]", CultureInfo.InvariantCulture, [Count("abc")]);

        // Assert
        Assert.Equal("0 files", evaluated);
    }

    [Fact]
    public void Should_report_an_argument_that_is_not_a_count_once_per_format_without_its_value()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other), loggerFactory.CreateLogger("plural"));

        // Act
        var first = evaluator.Evaluate(AllFields, CultureInfo.InvariantCulture, [Count("secret")]);
        var second = evaluator.Evaluate(AllFields, CultureInfo.InvariantCulture, [Count("secret")]);

        // Assert
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Multiple(() =>
        {
            Assert.Equal("0 other", first);
            Assert.Equal("0 other", second);
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Debug, entry.Level);
            Assert.Contains("count", entry.Message, StringComparison.Ordinal);
            Assert.Contains("String", entry.Message, StringComparison.Ordinal);
            Assert.Contains("not numeric", entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("secret", entry.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Should_report_an_argument_that_is_not_a_count_as_a_warning_under_diagnostics()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other), loggerFactory.CreateLogger("plural"), new LocalizationDiagnostics());

        // Act
        _ = evaluator.Evaluate(AllFields, CultureInfo.InvariantCulture, [Count("secret")]);

        // Assert
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Multiple(
            () => Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, entry.Level),
            () => Assert.Equal(1, entry.EventId.Id),
            () => Assert.Equal("count", entry.Fields["Argument"]));
    }

    [Fact]
    public void Should_reject_a_null_rule_set()
        => Assert.Throws<ArgumentNullException>(() => new LocaleExpressionEvaluator(null!));
    [Fact]
    public void Should_reject_a_null_format()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Assert
        Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate(null!, CultureInfo.InvariantCulture, []));
    }

    [Fact]
    public void Should_reject_a_null_culture()
    {
        // Arrange
        var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other));

        // Assert
        Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate(AllFields, null!, []));
    }

    public sealed class Gender
    {
        private const string Format = "[$ [Rider] gender; female: Escaladora; male: Escalador; other: Ciclista; $]";

        public enum RiderGender
        {
            Female,
            Male,
            Unstated,
        }

        private static LocaleExpressionEvaluator CreateEvaluator()
            => new(new StubPluralRuleSet(PluralCategory.Other));

        private static LocalizedArgument Rider(object? value)
            => new("Rider", value);

        [Theory]
        [InlineData(RiderGender.Female, "Escaladora")]
        [InlineData(RiderGender.Male, "Escalador")]
        public void Should_choose_the_field_an_enum_member_names(RiderGender gender, string expected)
        {
            // Act
            var evaluated = CreateEvaluator().Evaluate(Format, CultureInfo.InvariantCulture, [Rider(gender)]);

            // Assert
            Assert.Equal(expected, evaluated);
        }

        [Theory]
        [InlineData("female", "Escaladora")]
        [InlineData("FEMALE", "Escaladora")]
        [InlineData("Male", "Escalador")]
        public void Should_choose_the_field_a_string_names_whatever_its_case(string gender, string expected)
        {
            // Act
            var evaluated = CreateEvaluator().Evaluate(Format, CultureInfo.InvariantCulture, [Rider(gender)]);

            // Assert
            Assert.Equal(expected, evaluated);
        }

        [Theory]
        [InlineData(RiderGender.Unstated)]
        [InlineData("nonbinary")]
        [InlineData(null)]
        public void Should_choose_other_for_a_gender_without_a_field_of_its_own(object? gender)
        {
            // Act
            var evaluated = CreateEvaluator().Evaluate(Format, CultureInfo.InvariantCulture, [Rider(gender)]);

            // Assert
            Assert.Equal("Ciclista", evaluated);
        }

        [Fact]
        public void Should_fall_back_to_other_when_the_text_omits_the_named_field()
        {
            // Act
            var evaluated = CreateEvaluator().Evaluate(
                "[$ [Rider] gender; female: Escaladora; other: Escalador; $]",
                CultureInfo.InvariantCulture,
                [Rider(RiderGender.Male)]);

            // Assert
            Assert.Equal("Escalador", evaluated);
        }

        [Fact]
        public void Should_read_the_sole_argument_for_an_expression_naming_none()
        {
            // Act
            var evaluated = CreateEvaluator().Evaluate(
                "[$ gender; female: Escaladora; other: Escalador; $]",
                CultureInfo.InvariantCulture,
                [LocalizedArgument.Unnamed(RiderGender.Female)]);

            // Assert
            Assert.Equal("Escaladora", evaluated);
        }

        [Fact]
        public void Should_write_a_hash_in_a_field_as_written()
        {
            // Act
            var evaluated = CreateEvaluator().Evaluate(
                "[$ [Rider] gender; female: La #1; other: El #1; $]",
                CultureInfo.InvariantCulture,
                [Rider(RiderGender.Female)]);

            // Assert
            Assert.Equal("La #1", evaluated);
        }

        [Fact]
        public void Should_resolve_a_gender_and_a_plural_in_one_sentence()
        {
            // Act
            var evaluated = CreateEvaluator().Evaluate(
                "{Name} [$ [Rider] gender; female: ganó; other: ganó; $] [$ [Stages] plural; one: # etapa; other: # etapas; $], [$ [Rider] gender; female: invicta; other: invicto; $]",
                CultureInfo.InvariantCulture,
                [new LocalizedArgument("Name", "Ana"), Rider(RiderGender.Female), new LocalizedArgument("Stages", 3)]);

            // Assert
            Assert.Equal("{Name} ganó 3 etapas, invicta", evaluated);
        }

        [Fact]
        public void Should_report_a_gender_that_is_neither_an_enum_nor_a_string_once_per_format_without_its_value()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other), loggerFactory.CreateLogger("gender"));

            // Act
            var first = evaluator.Evaluate(Format, CultureInfo.InvariantCulture, [Rider(1)]);
            var second = evaluator.Evaluate(Format, CultureInfo.InvariantCulture, [Rider(1)]);

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal("Ciclista", first);
                Assert.Equal("Ciclista", second);
                Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Debug, entry.Level);
                Assert.Equal(9, entry.EventId.Id);
                Assert.Equal("Rider", entry.Fields["Argument"]);
                Assert.Equal("Int32", entry.Fields["ArgumentType"]);
                Assert.Contains("neither an enum nor a string", entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public void Should_report_a_gender_that_is_neither_an_enum_nor_a_string_as_a_warning_under_diagnostics()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other), loggerFactory.CreateLogger("gender"), new LocalizationDiagnostics());

            // Act
            _ = evaluator.Evaluate(Format, CultureInfo.InvariantCulture, [Rider(1)]);

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, entry.Level);
        }

        [Fact]
        public void Should_report_nothing_for_a_null_gender()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var evaluator = new LocaleExpressionEvaluator(new StubPluralRuleSet(PluralCategory.Other), loggerFactory.CreateLogger("gender"), new LocalizationDiagnostics());

            // Act
            _ = evaluator.Evaluate(Format, CultureInfo.InvariantCulture, [Rider(null)]);

            // Assert
            Assert.Empty(loggerFactory.Entries);
        }
    }
}
