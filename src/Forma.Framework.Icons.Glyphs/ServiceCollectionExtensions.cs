namespace Forma.Framework.Icons.Glyphs;

using Forma.Framework.Icons;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the artwork this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers Forma's own glyphs and brand marks as the icon provider, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaGlyphServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IIconProvider, GlyphIconProvider>();

        return services;
    }
}
