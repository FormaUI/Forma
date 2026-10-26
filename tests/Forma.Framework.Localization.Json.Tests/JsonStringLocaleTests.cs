namespace Forma.Framework.Localization.Json.Tests;

using Forma.Framework.Localization.Json.Tests.Fixtures;
using Forma.Framework.Localization.Json.Tests.Fixtures.Beside;

public sealed class JsonStringLocaleTests
{
    private static IStringLocale CreateLocale<T>()
        => new JsonStringLocaleFactory().Create(typeof(T));

    [Fact]
    public void Should_return_the_key_when_the_culture_has_no_file()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Colocated>();

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
        var strings = CreateLocale<Unlocalized>();

        // Act
        var localized = strings["Anything at all"];

        // Assert
        Assert.Equal("Anything at all", localized.Value);
    }

    [Fact]
    public void Should_localize_from_the_matching_culture_file()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Colocated>();

        // Act
        var localized = strings["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_unescape_a_doubled_brace_in_a_translation()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Colocated>();

        // Act
        var localized = strings["Use {{curly}} braces."];

        // Assert
        Assert.Equal("השתמשו בסוגריים {מסולסלים}.", localized.Value);
    }

    [Fact]
    public void Should_unescape_a_doubled_brace_in_a_key_it_falls_back_to()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Colocated>();

        // Act
        var localized = strings["Use {{curly}} braces."];

        // Assert
        Assert.Equal("Use {curly} braces.", localized.Value);
    }

    [Fact]
    public void Should_find_an_entry_whose_key_is_escaped_with_a_doubled_dollar()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Escaped>();

        // Act
        var localized = strings["$$price"];

        // Assert
        Assert.Multiple(
            () => Assert.Equal("מחיר", localized.Value),
            () => Assert.False(localized.ResourceNotFound));
    }

    [Fact]
    public void Should_unescape_a_doubled_dollar_in_a_key_it_falls_back_to()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Escaped>();

        // Act
        var localized = strings["$$price"];

        // Assert
        Assert.Multiple(
            () => Assert.Equal("$price", localized.Value),
            () => Assert.True(localized.ResourceNotFound));
    }

    [Fact]
    public void Should_drop_one_dollar_alone_from_a_key_with_several()
    {
        // Arrange — the key is unescaped once on each side, so a third sign survives in both the match and the fallback.
        var strings = CreateLocale<Escaped>();

        // Act
        string hebrew;
        string english;
        using (var _ = new CultureScope("he"))
        {
            hebrew = strings["$$$price"].Value;
        }

        using (var _ = new CultureScope("en"))
        {
            english = strings["$$$price"].Value;
        }

        // Assert
        Assert.Multiple(
            () => Assert.Equal("מחיר כפול", hebrew),
            () => Assert.Equal("$$price", english));
    }

    [Fact]
    public void Should_serve_an_escaped_reserved_name_as_an_ordinary_translation()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Escaped>();

        // Act
        var localized = strings["$$tools"];

        // Assert
        Assert.Equal("כלים", localized.Value);
    }

    [Fact]
    public void Should_find_an_entry_whose_key_starts_with_a_dollar_as_text()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Escaped>();

        // Act
        var localized = strings["$USD"];

        // Assert
        Assert.Equal("דולר", localized.Value);
    }

    [Fact]
    public void Should_fall_back_from_a_regional_culture_to_its_parent()
    {
        // Arrange
        using var culture = new CultureScope("es-419");
        var strings = CreateLocale<Colocated>();

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
        var strings = CreateLocale<Colocated>();

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
        var strings = CreateLocale<Colocated>();

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
        var strings = CreateLocale<Colocated>();

        // Act
        var localized = strings["No such string"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Should_format_values_into_the_key_when_untranslated()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Colocated>();

        // Act
        var localized = strings["Moved to position {position} of {total}.", 2, 7];

        // Assert
        Assert.Equal("Moved to position 2 of 7.", localized.Value);
    }

    [Fact]
    public void Should_pair_values_with_the_keys_holes_by_position()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Colocated>();

        // Act
        var localized = strings["Moved to position {position} of {total}.", 2, 7];

        // Assert
        Assert.Equal("הועבר למיקום 2 מתוך 7.", localized.Value);
    }

    [Fact]
    public void Should_keep_positional_values_with_their_holes_when_a_translation_reorders_them()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Colocated>();

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
        var strings = CreateLocale<Colocated>();
        var values = new object?[count];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = strings["Stored {stored} of {total}.", values]);
    }

    [Fact]
    public void Should_pair_values_with_the_keys_holes_when_resolving()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Colocated>();

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
        var strings = CreateLocale<GenericArity<int>>();

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
        var strings = CreateLocale<MainAssembly>();

        // Act
        var localized = strings["Close"];

        // Assert
        Assert.Equal("סגור", localized.Value);
    }

    [Fact]
    public void Should_read_a_culture_file_the_build_moves_into_a_satellite_assembly()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<InSatellite>();

        // Act
        var localized = strings["Close"];

        // Assert
        Assert.Equal("סגור", localized.Value);
    }

    [Fact]
    public void Should_reject_a_blank_key()
    {
        // Arrange
        var strings = CreateLocale<Colocated>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => strings[string.Empty]);
    }

    [Fact]
    public void Should_read_the_current_culture_on_every_lookup()
    {
        // Arrange
        var strings = CreateLocale<Colocated>();

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
    public void Should_look_a_bound_locales_strings_up_in_its_ui_culture()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Colocated>();

        // Act
        var localized = strings.WithCulture(CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("he"))["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_keep_locales_bound_from_one_original_apart_from_each_other_and_from_it()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Colocated>();

        // Act
        var hebrew = strings.WithCulture(CultureInfo.GetCultureInfo("he"), CultureInfo.GetCultureInfo("he"));
        var spanish = strings.WithCulture(CultureInfo.GetCultureInfo("es"), CultureInfo.GetCultureInfo("es"));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("היום", hebrew["Today"].Value);
            Assert.Equal("Hoy", spanish["Today"].Value);
            Assert.Equal("Today", strings["Today"].Value);
        });
    }

    [Fact]
    public void Should_replace_both_cultures_when_a_bound_locale_is_bound_again()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var hebrew = CreateLocale<Colocated>().WithCulture(CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("he"));

        // Act
        var rebound = hebrew.WithCulture(CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("es"));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("Hoy", rebound["Today"].Value);
            Assert.Equal("Stored 1.5 of 2.", rebound["Stored {stored} of {total}.", 1.5, 2].Value);
        });
    }

    [Fact]
    public void Should_change_only_the_values_when_only_the_formatting_culture_changes()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Colocated>();
        var english = CultureInfo.GetCultureInfo("en");

        // Act
        var inGerman = strings.WithCulture(CultureInfo.GetCultureInfo("de-DE"), english)["Stored {stored} of {total}.", 1.5, 2];
        var inEnglish = strings.WithCulture(english, english)["Stored {stored} of {total}.", 1.5, 2];

        // Assert
        Assert.Equal(["Stored 1,5 of 2.", "Stored 1.5 of 2."], [inGerman.Value, inEnglish.Value]);
    }

    [Fact]
    public void Should_change_only_the_language_and_plural_forms_when_only_the_ui_culture_changes()
    {
        // Arrange
        const string Key = "[$ plural; one: # file; other: # files; $]";
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Pluralized>();
        var english = CultureInfo.GetCultureInfo("en");

        // Act
        var inHebrew = strings.WithCulture(english, CultureInfo.GetCultureInfo("he"))[Key, 2];
        var inEnglish = strings.WithCulture(english, english)[Key, 2];

        // Assert
        Assert.Equal(["שני קבצים", "2 files"], [inHebrew.Value, inEnglish.Value]);
    }

    [Fact]
    public void Should_format_a_value_that_takes_no_culture_in_the_bound_cultures()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Colocated>().WithCulture(CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("en"));

        // Act
        var localized = strings["Stored {stored} of {total}.", new Measure(1.5), 2];

        // Assert
        Assert.Equal("Stored Measure { Size = 1,5 } of 2.", localized.Value);
    }

    [Fact]
    public void Should_list_the_strings_of_a_bound_locales_ui_culture()
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Colocated>().WithCulture(CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("he"));

        // Act
        var names = strings.GetAllStrings(includeParentCultures: false).Select(s => s.Name);

        // Assert
        Assert.Equal(
            ["Last 7 days", "Moved to position {position} of {total}.", "Stored {stored} of {total}.", "Today", "Use {{curly}} braces."],
            names.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Should_reject_a_missing_culture_to_bind()
    {
        // Arrange
        var strings = CreateLocale<Colocated>();
        var english = CultureInfo.GetCultureInfo("en");

        // Act & Assert
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => strings.WithCulture(null!, english)),
            () => Assert.Throws<ArgumentNullException>(() => strings.WithCulture(english, null!)));
    }

    [Fact]
    public void Should_list_the_strings_of_the_current_culture()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Colocated>();

        // Act
        var names = strings.GetAllStrings(includeParentCultures: false).Select(s => s.Name);

        // Assert
        Assert.Equal(
            ["Last 7 days", "Moved to position {position} of {total}.", "Stored {stored} of {total}.", "Today", "Use {{curly}} braces."],
            names.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Should_list_parent_culture_strings_when_asked()
    {
        // Arrange
        using var culture = new CultureScope("es-419");
        var strings = CreateLocale<Colocated>();

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
    public void Should_resolve_a_pattern_with_its_holes_left_open()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Colocated>();

        // Act
        var pattern = strings.Resolve("Moved to position {position} of {total}.", 2, 7);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("הועבר למיקום {position} מתוך {total}.", pattern.Format);
            Assert.Equal([new LocalizedArgument("position", 2), new LocalizedArgument("total", 7)], pattern.Arguments);
        });
    }

    [Fact]
    public void Should_evaluate_the_plural_expression_in_a_translation()
    {
        // Arrange
        const string Key = "[$ plural; one: # file; other: # files; $]";
        using var culture = new CultureScope("he");
        var strings = CreateLocale<Pluralized>();

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

    [Theory]
    [InlineData("[$ plural; one: # file; other: # files $]")]
    [InlineData("[$ select; male: his; female: her; $]")]
    public void Should_render_a_expression_it_cannot_evaluate_exactly_as_written(string key)
    {
        // Arrange
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Pluralized>();

        // Act
        var localized = strings[key, 7];

        // Assert
        Assert.Equal(key, localized.Value);
    }

    [Fact]
    public void Should_evaluate_the_plural_expression_in_the_key_when_untranslated()
    {
        // Arrange
        const string Key = "[$ plural; one: # file; other: # files; $]";
        using var culture = new CultureScope("en");
        var strings = CreateLocale<Pluralized>();

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
        var strings = new JsonStringLocaleFactory(new LocalizationOptions { SourceLanguage = "he" }).Create(typeof(Unlocalized));

        // Act
        var two = strings[Key, 2];

        // Assert
        Assert.Equal("שני פריטים", two.Value);
    }

    [Fact]
    public void Should_reject_a_blank_source_language()
        => Assert.Throws<ArgumentException>(() => new JsonStringLocaleFactory(new LocalizationOptions { SourceLanguage = " " }));

    [Fact]
    public void Should_reject_a_source_language_naming_no_language()
        => Assert.Throws<ArgumentException>(() => new JsonStringLocaleFactory(new LocalizationOptions { SourceLanguage = "x-test" }));

    [Fact]
    public void Should_read_a_regional_source_language_as_its_language()
    {
        // Arrange — the key is Hebrew, and only Hebrew's rules carry a dual.
        const string Key = "[$ plural; one: פריט אחד; two: שני פריטים; other: # פריטים; $]";
        using var culture = new CultureScope("en");
        var strings = new JsonStringLocaleFactory(new LocalizationOptions { SourceLanguage = "he-IL" }).Create(typeof(Unlocalized));

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
        var strings = new JsonStringLocaleFactory(new LocalizationOptions { SourceLanguage = "en" }).Create(typeof(Unlocalized));

        // Act
        var many = strings[Key, 1234.5m];

        // Assert
        Assert.Equal("1.234,5 files", many.Value);
    }

    private sealed record Measure(double Size);
}
