namespace Forma.Framework.Localization.Abstractions.Tests;

public sealed class PluralExpressionEvaluatorLanguageTests
{
    private const string English = "[$ [0] plural; one: # file; other: # files; $]";
    private const string Hebrew = "[$ [0] plural; one: קובץ אחד; two: שני קבצים; other: # קבצים; $]";
    private const string Arabic = "[$ [0] plural; zero: لا ملفات; one: ملف واحد; two: ملفان; few: # ملفات; many: # ملفًا; other: # ملف; $]";
    private const string French = "[$ [0] plural; one: # fichier; many: # millions de fichiers; other: # fichiers; $]";

    private static readonly PluralExpressionEvaluator Evaluator = new(CommonPluralRuleSet.Default);

    [Theory]
    [InlineData("en", English, 0, "0 files")]
    [InlineData("en", English, 1, "1 file")]
    [InlineData("en", English, 2, "2 files")]
    [InlineData("he", Hebrew, 1, "קובץ אחד")]
    [InlineData("he", Hebrew, 2, "שני קבצים")]
    [InlineData("he", Hebrew, 3, "3 קבצים")]
    [InlineData("ar", Arabic, 0, "لا ملفات")]
    [InlineData("ar", Arabic, 2, "ملفان")]
    [InlineData("ar", Arabic, 3, "3 ملفات")]
    [InlineData("ar", Arabic, 11, "11 ملفًا")]
    [InlineData("fr", French, 0, "0 fichier")]
    [InlineData("fr", French, 2, "2 fichiers")]
    public void Should_choose_the_field_the_language_selects(string language, string pattern, int count, string expected)
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo(language);

        // Act
        var evaluated = Evaluator.Evaluate(pattern, culture, [count]);

        // Assert
        Assert.Equal(expected, evaluated);
    }

    [Fact]
    public void Should_fall_back_to_other_when_the_language_needs_a_field_the_translation_omits()
    {
        // Act
        var evaluated = Evaluator.Evaluate(English, CultureInfo.GetCultureInfo("ar"), [11]);

        // Assert
        Assert.Equal("11 files", evaluated);
    }

    [Fact]
    public void Should_group_a_large_count_for_the_culture()
    {
        // Act
        var evaluated = Evaluator.Evaluate(English, CultureInfo.GetCultureInfo("en"), [1000000]);

        // Assert
        Assert.Equal("1,000,000 files", evaluated);
    }

    [Fact]
    public void Should_keep_the_digits_a_fractional_count_was_written_with()
    {
        // Act
        var evaluated = Evaluator.Evaluate(English, CultureInfo.GetCultureInfo("en"), [0.5m]);

        // Assert
        Assert.Equal("0.5 files", evaluated);
    }

    [Fact]
    public void Should_select_by_the_text_language_and_group_for_the_reader()
    {
        // Act
        var evaluated = Evaluator.Evaluate(English, CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("es-CO"), [1234.5m]);

        // Assert
        Assert.Equal("1.234,5 files", evaluated);
    }

    [Fact]
    public void Should_fall_back_to_other_when_the_text_names_no_language()
    {
        // Act
        var evaluated = Evaluator.Evaluate(English, CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("en"), [1]);

        // Assert
        Assert.Equal("1 files", evaluated);
    }
}
