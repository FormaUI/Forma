namespace Forma.Framework.Icons.Glyphs;

using Forma.Framework.Icons;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the artwork this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="GlyphIconProvider"/> as the icon provider.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaGlyphServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IIconProvider, GlyphIconProvider>();

        return services;
    }
}
