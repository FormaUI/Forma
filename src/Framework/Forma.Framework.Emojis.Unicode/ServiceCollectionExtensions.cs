namespace Forma.Framework.Emojis.Unicode;

using Forma.Framework.Emojis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the emoji services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="UnicodeEmojiProvider"/> as the emoji provider.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaEmojiServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IEmojiProvider, UnicodeEmojiProvider>();

        return services;
    }
}
