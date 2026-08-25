namespace Forma.UI.Blazor.Icons;

using Forma.UI.Style.Icons;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the artwork <see cref="Components.FormaIcon" /> draws.</summary>
public static class IconServiceCollectionExtensions
{
    /// <summary>Registers the artwork this library ships, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaIcons(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IIconProvider, DefaultIconProvider>();

        return services;
    }
}
