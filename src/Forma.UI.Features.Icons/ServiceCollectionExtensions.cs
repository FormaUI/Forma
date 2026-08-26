namespace Forma.UI.Features.Icons;

using Forma.UI.Design.Icons;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the glyph artwork this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the glyph artwork this package ships, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaIcons(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IIconProvider, DefaultIconProvider>();

        return services;
    }
}
