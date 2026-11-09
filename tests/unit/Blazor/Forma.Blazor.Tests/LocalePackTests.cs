namespace Forma.Blazor.Tests;

using Microsoft.Extensions.Logging.Abstractions;

public sealed class LocalePackTests
{
    [Fact]
    public void Should_record_the_packs_the_shared_context_references_in_its_own_assembly()
    {
        // Arrange — this project references the packs directly, so its assembly records them.
        var assembly = typeof(LocalePackTests).Assembly;

        // Act
        var names = LocaleAssemblyLoader.GetPackNames(assembly);

        // Assert
        Assert.Equal(["Forma.Blazor.Locales.Hebrew", "Forma.Blazor.Locales.Spanish"], names);
    }

    [Fact]
    public void Should_load_every_pack_recorded()
    {
        // Arrange
        var assembly = typeof(LocalePackTests).Assembly;

        // Act
        var packs = LocaleAssemblyLoader.LoadPacks(assembly, NullLogger.Instance);

        // Assert
        Assert.Equal(["Forma.Blazor.Locales.Hebrew", "Forma.Blazor.Locales.Spanish"], packs.Select(pack => pack.GetName().Name));
    }
}
