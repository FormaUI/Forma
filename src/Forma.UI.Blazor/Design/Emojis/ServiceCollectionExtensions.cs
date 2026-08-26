namespace Forma.UI.Blazor.Design.Emojis;

using Forma.UI.Design.Emojis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the emoji <see cref="Components.FormaEmoji" /> draws and <see cref="Components.Inputs.FormaEmojiPicker" /> offers.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the emoji this library ships, unless a host has already registered its own.</summary>
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
