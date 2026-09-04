namespace Forma.Framework.Icons.Glyphs.Tests;

using Forma.Framework.Icons;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_glyphs_as_the_icon_provider()
    {
        // Act
        var services = new ServiceCollection().AddFormaGlyphServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IIconProvider));
        Assert.Multiple(
            () => Assert.Equal(typeof(GlyphIconProvider), descriptor.ImplementationType),
            () => Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime));
    }
}
