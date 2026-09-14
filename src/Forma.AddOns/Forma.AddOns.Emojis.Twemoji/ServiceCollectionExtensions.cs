namespace Forma.Framework.Emojis.Twemoji;

using Forma.Framework.Emojis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the emoji artwork and catalog this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the emoji this package ships, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaTwemojiPack(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IEmojiProvider, TwemojiEmojiProvider>();
        services.TryAddSingleton<IEmojiCatalog, TwemojiEmojiCatalog>();

        return services;
    }
}
