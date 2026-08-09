namespace Forma.Framework.Localization.Tests;

using Forma.Framework.Localization.Tests.Sources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

public sealed class JsonStringLocalizerFactoryTests
{
    [Fact]
    public void Should_read_files_from_the_configured_resources_folder()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocalizerFactory(new JsonLocalizationOptions { ResourcesPath = "Resources" });

        // Act
        var localized = factory.Create(typeof(PathScoped))["Save"];

        // Assert
        Assert.Equal("שמור", localized.Value);
    }

    [Fact]
    public void Should_not_find_a_colocated_file_when_a_resources_folder_is_configured()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocalizerFactory(new JsonLocalizationOptions { ResourcesPath = "Resources" });

        // Act
        var localized = factory.Create(typeof(Colocated))["Today"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Should_return_the_same_localizer_for_the_same_type()
    {
        // Arrange
        var factory = new JsonStringLocalizerFactory();

        // Act
        var first = factory.Create(typeof(Colocated));
        var second = factory.Create(typeof(Colocated));

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Should_create_a_localizer_from_a_base_name_and_assembly()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocalizerFactory();

        // Act
        var localized = factory.Create("Sources.Colocated", typeof(Colocated).Assembly.GetName().Name!)["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    // A project whose assembly name is not its root namespace embeds under the root, which the type carries.
    [Fact]
    public void Should_read_a_file_embedded_under_a_root_the_assembly_name_does_not_lead()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocalizerFactory();

        // Act
        var localized = factory.Create(typeof(Contoso.Widgets.ForeignRoot))["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    // A type outside any namespace carries no root, so the assembly name has to supply one.
    [Fact]
    public void Should_read_a_file_for_a_type_declared_without_a_namespace()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var factory = new JsonStringLocalizerFactory();

        // Act
        var localized = factory.Create(typeof(GlobalRoot))["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_reject_a_null_resource_source()
    {
        // Arrange
        var factory = new JsonStringLocalizerFactory();

        // Act
        var act = () => factory.Create(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Should_resolve_a_typed_localizer_through_the_container()
    {
        // Arrange
        using var culture = new CultureScope("he");
        var services = new ServiceCollection().AddJsonLocalization().BuildServiceProvider();

        // Act
        var localized = services.GetRequiredService<IStringLocalizer<Colocated>>()["Today"];

        // Assert
        Assert.Equal("היום", localized.Value);
    }

    [Fact]
    public void Should_let_a_host_replace_the_factory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStringLocalizerFactory>(new JsonStringLocalizerFactory(new JsonLocalizationOptions { ResourcesPath = "Resources" }));

        // Act
        using var provider = services.AddJsonLocalization().BuildServiceProvider();
        using var culture = new CultureScope("he");
        var localized = provider.GetRequiredService<IStringLocalizer<PathScoped>>()["Save"];

        // Assert
        Assert.Equal("שמור", localized.Value);
    }
}
