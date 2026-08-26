namespace Forma.UI.Features.Emojis;

using Forma.UI.Design.Emojis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the emoji artwork and catalog this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the emoji this package ships, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaEmojis(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IEmojiProvider, DefaultEmojiProvider>();
        services.TryAddSingleton<IEmojiCatalog, DefaultEmojiCatalog>();

        return services;
    }
}
