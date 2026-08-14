namespace Forma.Framework.Localization.Json.Tests;

using Forma.Framework.Localization.Json.Tests.Fixtures;
using Forma.Tests.Shared;

public sealed class RequireMatchingLocalizationTests
{
    private static IStringLocale Locale<TSource>(bool require)
        => new JsonStringLocaleFactory(new JsonLocalizationOptions { RequireMatchingLocalization = require }).Create(typeof(TSource));

    [Fact]
    public void Should_fall_back_to_the_key_when_a_translation_is_not_required()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>(require: false);

        // Act
        var localized = strings["Absent from every file"];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("Absent from every file", localized.Value);
            Assert.True(localized.ResourceNotFound);
        });
    }

    [Fact]
    public void Should_throw_for_a_key_with_no_translation()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>(require: true);

        // Act
        var thrown = Assert.Throws<LocalizationNotFoundException>(() => strings["Absent from every file"]);

        // Assert
        Assert.Contains("Absent from every file", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_throw_for_a_key_with_arguments_and_no_translation()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>(require: true);

        // Act, Assert
        Assert.Throws<LocalizationNotFoundException>(() => strings["Absent from every file", 2]);
    }

    [Fact]
    public void Should_throw_from_resolve_for_a_key_with_no_translation()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>(require: true);

        // Act, Assert
        Assert.Throws<LocalizationNotFoundException>(() => strings.Resolve("Absent from every file"));
    }

    [Fact]
    public void Should_throw_for_an_interpolated_key_with_no_translation()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>(require: true);
        var count = 2;

        // Act, Assert
        Assert.Throws<LocalizationNotFoundException>(() => strings[$"Absent from every file, {count} times"]);
    }

    [Fact]
    public void Should_throw_from_resolve_for_an_interpolated_key_with_no_translation()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>(require: true);
        var count = 2;

        // Act, Assert
        Assert.Throws<LocalizationNotFoundException>(() => strings.Resolve($"Absent from every file, {count} times"));
    }

    [Fact]
    public void Should_not_throw_for_a_key_that_is_translated()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>(require: true);

        // Act
        var localized = strings["Today"];

        // Assert
        Assert.False(localized.ResourceNotFound);
    }

    [Fact]
    public void Should_list_every_string_without_throwing()
    {
        // Arrange — a survey reports what is there rather than demanding what is not.
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>(require: true);

        // Act
        var all = strings.GetAllStrings(includeParentCultures: false).ToList();

        // Assert
        Assert.NotEmpty(all);
    }

    [Fact]
    public void Should_read_a_null_translation_as_the_key_itself()
    {
        // Arrange — the file writes null rather than repeating the key.
        using var culture = new CultureScope("he");
        var strings = Locale<NullValued>(require: true);

        // Act
        var localized = strings["Save"];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("Save", localized.Value);
            Assert.False(localized.ResourceNotFound);
        });
    }

    [Fact]
    public void Should_list_a_null_translation_as_the_key_itself()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<NullValued>(require: false);

        // Act
        var listed = strings.GetAllStrings(includeParentCultures: false).Single(entry => entry.Name == "Save");

        // Assert
        Assert.Equal("Save", listed.Value);
    }

    [Fact]
    public void Should_inflect_a_null_translation_by_the_source_language()
    {
        // Arrange — French counts zero as singular, but the key it stands for is English.
        using var culture = new CultureScope("fr");
        var strings = Locale<NullValued>(require: false);

        // Act
        var none = strings["[$ [0] plural; one: # file; other: # files; $]", 0];

        // Assert
        Assert.Equal("0 files", none.Value);
    }
}
