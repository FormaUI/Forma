namespace Forma.Framework.Themes.Browser.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the theme coordination this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the theme system, the registration that keeps each user scope's theme state on the choice the page's theme coordinator accepts, and the switch that requests through it.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaThemeCoordination(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaThemes();
        services.AddLogging();
        services.TryAddScoped(ThemeCoordinatorRegistration.Create);
        services.TryAddScoped<IThemeSwitch>(provider => new CoordinatedThemeSwitch(provider.GetRequiredService<ThemeCoordinatorRegistration>()));

        return services;
    }
}
