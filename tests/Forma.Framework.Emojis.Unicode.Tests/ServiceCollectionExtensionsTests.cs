namespace Forma.Framework.Emojis.Unicode.Tests;

using Forma.Framework.Emojis;

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

    [Fact]
    public void Should_register_the_reader_font_as_the_emoji_provider()
    {
        // Act
        var services = new ServiceCollection().AddFormaEmojiServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IEmojiProvider));
        Assert.Multiple(
            () => Assert.Equal(typeof(UnicodeEmojiProvider), descriptor.ImplementationType),
            () => Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime));
    }
}
