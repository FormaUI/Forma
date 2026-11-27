namespace Forma.Framework.Themes.Blazor.WebAssembly;

using Forma.Framework.Themes.Browser.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>Registers the theme services a WebAssembly client needs.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the runtime's theme state, its registration with the page's theme coordinator at startup, and the switch that requests through it.</summary>
    /// <remarks>A WebAssembly runtime is one user scope in one process, so each of these is one per runtime: the startup, which the runtime runs outside the scope its components resolve from, registers the state they read.</remarks>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaThemeWebAssemblyIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Ahead of the component library's registrations, which then keep these.
        services.AddFormaThemeState(ServiceLifetime.Singleton);
        services.TryAddSingleton(ThemeCoordinatorRegistration.Create);
        services.TryAddSingleton<IThemeSwitch>(provider => new CoordinatedThemeSwitch(provider.GetRequiredService<ThemeCoordinatorRegistration>()));
        services.AddFormaThemeCoordination();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ThemeCoordinatorStartup>(provider => new ThemeCoordinatorStartup(provider.GetRequiredService<ThemeCoordinatorRegistration>())));

        return services;
    }
}
