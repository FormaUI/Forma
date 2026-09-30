namespace Forma.Framework.Localization.Json.Tests;

using Forma.Framework.Localization.Json.Tests.Fixtures;

using Microsoft.Extensions.DependencyInjection;

public sealed class JsonStringLocaleFactoryTests
{
    [Fact]
    public void Should_read_files_from_a_folder_by_the_path_its_settings_name()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocaleFactory();

        // Act
        var localized = factory.Create(typeof(PathScoped))["Save"];

        // Assert
        Assert.Equal("שמור", localized.Value);
    }

    [Fact]
    public void Should_keep_reading_a_file_beside_its_type_outside_that_folder()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocaleFactory();

        // Act
        var localized = factory.Create(typeof(Colocated))["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_return_the_same_locale_for_the_same_type()
    {
        // Arrange
        var factory = new JsonStringLocaleFactory();

        // Act
        var first = factory.Create(typeof(Colocated));
        var second = factory.Create(typeof(Colocated));

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Should_create_a_locale_from_a_base_name_and_assembly()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocaleFactory();

        // Act
        var localized = factory.Create("Fixtures.Colocated", typeof(Colocated).Assembly.GetName().Name!)["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_read_a_file_embedded_under_a_root_the_assembly_name_does_not_lead()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocaleFactory();

        // Act
        var localized = factory.Create(typeof(Contoso.Widgets.ForeignRoot))["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_read_a_file_for_a_type_declared_without_a_namespace()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocaleFactory();

        // Act
        var localized = factory.Create(typeof(GlobalRoot))["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_reject_a_null_resource_source()
    {
        // Arrange
        var factory = new JsonStringLocaleFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    [Fact]
    public void Should_resolve_a_typed_locale_through_the_container()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var services = new ServiceCollection().AddJsonLocalization().BuildServiceProvider();

        // Act
        var localized = services.GetRequiredService<IStringLocale<Colocated>>()["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_let_a_host_replace_the_factory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStringLocaleFactory>(new JsonStringLocaleFactory(new LocalizationOptions { RequireMatchingLocalization = true }));

        // Act
        using var provider = services.AddJsonLocalization().BuildServiceProvider();
        using var culture = new CultureScope("he");
        var strings = provider.GetRequiredService<IStringLocale<Colocated>>();

        // Assert — only the host's factory refuses a key no file translates.
        Assert.Throws<LocalizationNotFoundException>(() => strings["No file translates this"]);
    }
}
