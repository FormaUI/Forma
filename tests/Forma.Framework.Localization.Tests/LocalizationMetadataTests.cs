namespace Forma.Framework.Localization.Tests;

using Forma.Framework.Localization.Tests.Fixtures;
using Forma.Tests.Shared;

public sealed class LocalizationMetadataTests
{
    [Fact]
    public void Should_reject_a_file_carrying_no_metadata()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Unmarked>();

        // Act
        var thrown = Assert.Throws<LocalizationMetadataException>(() => strings["Save"]);

        // Assert
        Assert.Contains("$localization", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_reject_metadata_that_is_not_a_field_list()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<MalformedMetadata>();

        // Assert
        Assert.Throws<LocalizationMetadataException>(() => strings["Save"]);
    }

    // The major part is the whole compatibility decision, so it names both versions to be actionable.
    [Fact]
    public void Should_reject_a_schema_major_this_build_does_not_read()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<FutureSchema>();

        // Act
        var thrown = Assert.Throws<LocalizationMetadataException>(() => strings["Save"]);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Contains("2.0.0", thrown.Message, StringComparison.Ordinal);
            Assert.Contains("1.x", thrown.Message, StringComparison.Ordinal);
        });
    }

    // A minor bump is additive by definition, so a file from a newer toolchain still reads here.
    [Fact]
    public void Should_read_a_later_minor_schema_and_ignore_what_it_adds()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<NewerMinor>();

        // Act
        var localized = strings["Save"];

        // Assert
        Assert.Equal("שמור", localized.Value);
    }

    [Fact]
    public void Should_reject_a_culture_the_file_name_does_not_give_it()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<MismatchedCulture>();

        // Act
        var thrown = Assert.Throws<LocalizationMetadataException>(() => strings["Save"]);

        // Assert
        Assert.Contains("es", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_keep_the_metadata_entry_out_of_lookups()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var localized = strings["$localization"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Should_keep_the_metadata_entry_out_of_every_string()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = Locale<Colocated>();

        // Act
        var all = strings.GetAllStrings(includeParentCultures: true);

        // Assert
        Assert.DoesNotContain(all, localized => localized.Name == "$localization");
    }

    // Every file names its own culture, so what a host calls primary never enters the check.
    [Fact]
    public void Should_read_a_file_whatever_the_host_calls_its_primary_culture()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var strings = new JsonStringLocaleFactory(new JsonLocalizationOptions { SourceLanguage = "es" }).Create(typeof(Colocated));

        // Act
        var localized = strings["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    private static IStringLocale Locale<TSource>()
        => new JsonStringLocaleFactory(new JsonLocalizationOptions()).Create(typeof(TSource));
}
