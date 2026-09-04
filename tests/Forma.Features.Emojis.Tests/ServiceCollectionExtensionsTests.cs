namespace Forma.Features.Emojis.Tests;

using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_sprite_loader_once_per_circuit()
    {
        // Act
        var services = new ServiceCollection().AddFormaEmojiServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(EmojiSpriteLoader));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
