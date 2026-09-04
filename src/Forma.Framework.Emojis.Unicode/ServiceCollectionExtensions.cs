namespace Forma.Framework.Emojis.Unicode;

using Forma.Framework.Emojis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the emoji services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the reader's font as the emoji provider, unless a host has already registered its own, and the sprite loader that adopts emoji sheets into each circuit's document.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaEmojiServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Scoped, because the sheets it has adopted are the ones in this circuit's own document.
        services.TryAddScoped<EmojiSpriteLoader>();

        services.TryAddSingleton<IEmojiProvider, UnicodeEmojiProvider>();

        return services;
    }
}
