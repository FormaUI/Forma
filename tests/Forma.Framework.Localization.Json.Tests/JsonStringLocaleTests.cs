namespace Forma.Framework.Localization.Json.Tests;

using Forma.Framework.Localization.Json.Tests.Fixtures;

public sealed class JsonStringLocaleTests
{
    private static IStringLocale Locale<T>()
        => new JsonStringLocaleFactory().Create(typeof(T));

    [Fact]
    public void Should_return_the_key_when_the_culture_has_no_file()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["Today"];

        // Assert
        Assert.Equal("Today", localized.Value);
    }

    [Fact]
    public void Should_return_the_key_when_the_type_has_no_file_at_all()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Untranslated>();

        // Act
        var localized = strings["Anything at all"];

        // Assert
        Assert.Equal("Anything at all", localized.Value);
    }

    [Fact]
    public void Should_translate_from_the_matching_culture_file()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_fall_back_from_a_regional_culture_to_its_parent()
    {
        // Arrange
        using var culture = new CultureScope("es-419");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["Today"];

        // Assert
        Assert.Equal("Hoy", localized.Value);
    }

    [Fact]
    public void Should_fall_back_to_the_key_for_a_key_missing_from_the_culture_file()
    {
        // Arrange
        using var culture = new CultureScope("es-419");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["Moved to position {position} of {total}."];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Should_report_a_translated_key_as_found()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["Today"];

        // Assert
        Assert.False(localized.ResourceNotFound);
    }

    [Fact]
    public void Should_report_an_unknown_key_as_not_found()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["No such string"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Should_format_arguments_into_the_translation()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["Moved to position {position} of {total}.", ("position", 2), ("total", 7)];

        // Assert
        Assert.Equal("הועבר למיקום 2 מתוך 7.", localized.Value);
    }

    [Fact]
    public void Should_format_arguments_into_the_key_when_untranslated()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["Moved to position {position} of {total}.", ("position", 2), ("total", 7)];

        // Assert
        Assert.Equal("Moved to position 2 of 7.", localized.Value);
    }

    [Fact]
    public void Should_pair_values_with_the_keys_holes_by_position()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["Moved to position {position} of {total}.", 2, 7];

        // Assert
        Assert.Equal("הועבר למיקום 2 מתוך 7.", localized.Value);
    }

    // Only the key decides which value is which, so a translation is free to read in another order.
    [Fact]
    public void Should_keep_positional_values_with_their_holes_when_a_translation_reorders_them()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["Stored {stored} of {total}.", 3, 9];

        // Assert
        Assert.Equal("מתוך 9 נשמרו 3.", localized.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Should_reject_a_value_count_the_key_does_not_carry_holes_for(int count)
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();
        var values = new object?[count];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = strings["Stored {stored} of {total}.", values]);
    }

    [Fact]
    public void Should_pair_values_with_the_keys_holes_when_resolving()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var pattern = strings.Resolve("Moved to position {position} of {total}.", 2, 7);

        // Assert
        Assert.Equal([new LocalizedArgument("position", 2), new LocalizedArgument("total", 7)], pattern.Arguments);
    }

    [Fact]
    public void Should_drop_the_generic_arity_from_the_file_name()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<GenericArity<int>>();

        // Act
        var localized = strings["Search"];

        // Assert
        Assert.Equal("חיפוש", localized.Value);
    }

    [Fact]
    public void Should_read_a_culture_file_kept_in_the_main_assembly()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<MainAssembly>();

        // Act
        var localized = strings["Close"];

        // Assert
        Assert.Equal("סגור", localized.Value);
    }

    [Fact]
    public void Should_reject_a_blank_key()
    {
        // Arrange
        var strings = Locale<Colocated>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strings[string.Empty]);
    }

    [Fact]
    public void Should_read_the_current_culture_on_every_lookup()
    {
        // Arrange
        var strings = Locale<Colocated>();

        // Act
        string hebrew;
        string spanish;
        using (var _ = new CultureScope("he"))
        {
            hebrew = strings["Today"].Value;
        }

        using (var _ = new CultureScope("es"))
        {
            spanish = strings["Today"].Value;
        }

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("היום", hebrew);
            Assert.Equal("Hoy", spanish);
        });
    }

    [Fact]
    public void Should_list_the_strings_of_the_current_culture()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var names = strings.GetAllStrings(includeParentCultures: false).Select(s => s.Name);

        // Assert
        Assert.Equal(
            ["Last 7 days", "Moved to position {position} of {total}.", "Stored {stored} of {total}.", "Today"],
            names.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Should_list_parent_culture_strings_when_asked()
    {
        // Arrange
        using var culture = new CultureScope("es-419");
        var strings = Locale<Colocated>();

        // Act
        var withParents = strings.GetAllStrings(includeParentCultures: true).Select(s => s.Name);
        var withoutParents = strings.GetAllStrings(includeParentCultures: false);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Contains("Today", withParents);
            Assert.Empty(withoutParents);
        });
    }

    [Fact]
    public void Should_format_an_interpolated_string_through_the_handler()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var position = 2;
        var total = 7;
        var localized = strings[$"Moved to position {position} of {total}."];

        // Assert
        Assert.Equal("הועבר למיקום 2 מתוך 7.", localized.Value);
    }

    [Fact]
    public void Should_resolve_a_pattern_with_its_holes_left_open()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var pattern = strings.Resolve("Moved to position {position} of {total}.", ("position", 2), ("total", 7));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("הועבר למיקום {position} מתוך {total}.", pattern.Format);
            Assert.Equal([new LocalizedArgument("position", 2), new LocalizedArgument("total", 7)], pattern.Arguments);
        });
    }

    [Fact]
    public void Should_resolve_an_interpolated_pattern_with_its_holes_left_open()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var position = 2;
        var total = 7;
        var pattern = strings.Resolve($"Moved to position {position} of {total}.");

        // Assert
        Assert.Equal("הועבר למיקום {position} מתוך {total}.", pattern.Format);
    }

    [Fact]
    public void Should_evaluate_the_plural_expression_in_a_translation()
    {
        // Arrange
        const string Key = "[$ plural; one: # file; other: # files; $]";
        using var culture = new CultureScope("he");
        var strings = Locale<Pluralized>();

        // Act
        var one = strings[Key, 1];
        var two = strings[Key, 2];
        var many = strings[Key, 5];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("קובץ אחד", one.Value);
            Assert.Equal("שני קבצים", two.Value);
            Assert.Equal("5 קבצים", many.Value);
        });
    }

    // The expression survives interpolation because the handler doubles only braces, which the delimiters avoid.
    [Fact]
    public void Should_evaluate_a_plural_expression_written_as_an_interpolated_string()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Pluralized>();
        const int count = 2;

        // Act
        var localized = strings[$"[$ [{count}] plural; one: # file; other: # files; $]"];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("שני קבצים", localized.Value);
            Assert.False(localized.ResourceNotFound);
        });
    }

    // A bracketed name carries no braces, so formatting cannot mistake it for a hole and rewrite it.
    [Theory]
    [InlineData("[$ plural; one: # file; other: # files $]")]
    [InlineData("[$ select; male: his; female: her; $]")]
    public void Should_render_a_expression_it_cannot_evaluate_exactly_as_written(string key)
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = Locale<Pluralized>();

        // Act
        var localized = strings[key, 7];

        // Assert
        Assert.Equal(key, localized.Value);
    }

    // Only a expression's own header is canonicalized, so brackets in prose keep the hole they mean to fill.
    [Fact]
    public void Should_fill_a_hole_inside_brackets_that_are_not_a_expression()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = Locale<Pluralized>();

        // Act
        var id = 7;
        var localized = strings[$"See [{id}] for details"];

        // Assert
        Assert.Equal("See [7] for details", localized.Value);
    }

    [Fact]
    public void Should_evaluate_the_plural_expression_in_the_key_when_untranslated()
    {
        // Arrange
        const string Key = "[$ plural; one: # file; other: # files; $]";
        using var culture = new CultureScope("en");
        var strings = Locale<Pluralized>();

        // Act
        var one = strings[Key, 1];
        var many = strings[Key, 3];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("1 file", one.Value);
            Assert.Equal("3 files", many.Value);
        });
    }

    [Fact]
    public void Should_evaluate_an_untranslated_key_with_the_source_language_rules()
    {
        // Arrange — the key is Hebrew, whose dual the displayed language does not have.
        const string Key = "[$ plural; one: פריט אחד; two: שני פריטים; other: # פריטים; $]";
        using var culture = new CultureScope("en");
        var strings = new JsonStringLocaleFactory(new JsonLocalizationOptions { SourceLanguage = "he" }).Create(typeof(Untranslated));

        // Act
        var two = strings[Key, 2];

        // Assert
        Assert.Equal("שני פריטים", two.Value);
    }

    [Fact]
    public void Should_reject_a_blank_source_language()
        => Assert.Throws<ArgumentException>(() => new JsonStringLocaleFactory(new JsonLocalizationOptions { SourceLanguage = " " }));

    [Fact]
    public void Should_reject_a_source_language_naming_no_language()
        => Assert.Throws<ArgumentException>(() => new JsonStringLocaleFactory(new JsonLocalizationOptions { SourceLanguage = "x-test" }));

    [Fact]
    public void Should_read_a_regional_source_language_as_its_language()
    {
        // Arrange — the key is Hebrew, and only Hebrew's rules carry a dual.
        const string Key = "[$ plural; one: פריט אחד; two: שני פריטים; other: # פריטים; $]";
        using var culture = new CultureScope("en");
        var strings = new JsonStringLocaleFactory(new JsonLocalizationOptions { SourceLanguage = "he-IL" }).Create(typeof(Untranslated));

        // Act
        var two = strings[Key, 2];

        // Assert
        Assert.Equal("שני פריטים", two.Value);
    }

    [Fact]
    public void Should_format_an_untranslated_count_for_the_reader()
    {
        // Arrange — the key is English, but its count belongs to the culture on screen.
        const string Key = "[$ plural; one: # file; other: # files; $]";
        using var culture = new CultureScope("es-CO");
        var strings = new JsonStringLocaleFactory(new JsonLocalizationOptions { SourceLanguage = "en" }).Create(typeof(Untranslated));

        // Act
        var many = strings[Key, 1234.5m];

        // Assert
        Assert.Equal("1.234,5 files", many.Value);
    }
}
