namespace Forma.Framework.Localization.Tests;

using Forma.Framework.Localization.Adapter;
using Forma.Tests.Localization;

using Microsoft.Extensions.Localization;

public sealed class StringLocalizerAdapterTests
{
    private static readonly Dictionary<string, string> Translations = new(StringComparer.Ordinal)
    {
        ["Save"] = "שמור",
        ["Stored {0} of {1}."] = "נשמרו {0} מתוך {1}.",
    };

    [Fact]
    public void Should_present_a_translation_as_a_localized_string()
    {
        // Arrange
        var localizer = new StringLocalizerAdapter(new StubStringLocale(Translations));

        // Act
        var localized = localizer["Save"];

        // Assert
        Assert.Equal("Save", localized.Name);
        Assert.Equal("שמור", localized.Value);
        Assert.False(localized.ResourceNotFound);
    }

    [Fact]
    public void Should_format_a_translation_with_its_arguments()
    {
        // Arrange
        var localizer = new StringLocalizerAdapter(new StubStringLocale(Translations));

        // Act
        var localized = localizer["Stored {0} of {1}.", 3, 4];

        // Assert
        Assert.Equal("נשמרו 3 מתוך 4.", localized.Value);
    }

    [Fact]
    public void Should_report_a_missing_translation_as_not_found()
    {
        // Arrange
        var localizer = new StringLocalizerAdapter(new StubStringLocale(Translations));

        // Act
        var localized = localizer["Absent"];

        // Assert
        Assert.True(localized.ResourceNotFound);
        Assert.Equal("Absent", localized.Value);
    }

    [Fact]
    public void Should_enumerate_every_translation()
    {
        // Arrange
        var localizer = new StringLocalizerAdapter(new StubStringLocale(Translations));

        // Act
        var names = localizer.GetAllStrings(includeParentCultures: true).Select(localized => localized.Name).ToList();

        // Assert
        Assert.Contains("Save", names);
        Assert.Contains("Stored {0} of {1}.", names);
    }

    [Fact]
    public void Should_be_consumable_as_the_contract_a_host_injects()
    {
        // Arrange
        var localizer = new StringLocalizerAdapter(new StubStringLocale(Translations));

        // Act
        var value = ReadThroughContract(localizer, "Save");

        // Assert
        Assert.Equal("שמור", value);
    }

    [Fact]
    public void Should_be_consumable_as_the_generic_contract_a_host_injects()
    {
        // Arrange
        var localizer = new StringLocalizerAdapter<StringLocalizerAdapterTests>(
            new StubStringLocale<StringLocalizerAdapterTests>(Translations));

        // Act
        var value = ReadThroughContract(localizer, "Save");

        // Assert
        Assert.Equal("שמור", value);
    }

    [Fact]
    public void Should_create_localizers_through_the_factory_a_host_resolves()
    {
        // Arrange
        var factory = new StringLocalizerFactoryAdapter(new StubStringLocaleFactory(Translations));

        // Act
        var byType = factory.Create(typeof(StringLocalizerAdapterTests));
        var byName = factory.Create("Anything", "Anywhere");

        // Assert
        Assert.Equal("שמור", byType["Save"].Value);
        Assert.Equal("שמור", byName["Save"].Value);
    }

    [Fact]
    public void Should_carry_the_searched_location_across()
    {
        // Arrange
        var localizer = new StringLocalizerAdapter(new StubStringLocale(Translations, "Somewhere.resx"));

        // Act
        var localized = localizer["Absent"];
        var enumerated = localizer.GetAllStrings(includeParentCultures: true).First();

        // Assert
        Assert.Equal("Somewhere.resx", localized.SearchedLocation);
        Assert.Equal("Somewhere.resx", enumerated.SearchedLocation);
    }

    [Fact]
    public void Should_reject_a_null_locale()
        => Assert.Throws<ArgumentNullException>(() => new StringLocalizerAdapter(null!));

    [Fact]
    public void Should_reject_a_null_generic_locale()
        => Assert.Throws<ArgumentNullException>(() => new StringLocalizerAdapter<StringLocalizerAdapterTests>(null!));

    [Fact]
    public void Should_reject_a_null_factory()
        => Assert.Throws<ArgumentNullException>(() => new StringLocalizerFactoryAdapter(null!));

    [Fact]
    public void Should_reject_a_null_resource_source()
    {
        // Arrange
        var factory = new StringLocalizerFactoryAdapter(new StubStringLocaleFactory(Translations));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!, "Anywhere"));
        Assert.Throws<ArgumentNullException>(() => factory.Create("Anything", null!));
    }

    private static string ReadThroughContract(IStringLocalizer localizer, string name)
        => localizer[name];
}
