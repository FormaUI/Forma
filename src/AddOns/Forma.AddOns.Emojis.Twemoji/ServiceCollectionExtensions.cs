namespace Forma.Framework.Emojis.Twemoji;

using Forma.Framework.Emojis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the emoji artwork and catalog this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the emoji provider and catalog this package ships.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaTwemojiPack(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IEmojiProvider, TwemojiEmojiProvider>();
        services.TryAddSingleton<IEmojiCatalog, TwemojiEmojiCatalog>();

        return services;
    }
}
