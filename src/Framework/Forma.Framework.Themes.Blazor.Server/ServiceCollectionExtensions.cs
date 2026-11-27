namespace Forma.Framework.Themes.Blazor.Server;

using Forma.Framework.Themes.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the server's half of the theme system.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the server's half: the theme state a request's scope starts in the request's choice, the choice <paramref name="givenChoice"/> gives a request, a circuit's registration with the page's theme coordinator from each connection, and the switch that requests through it.</summary>
    /// <remarks>The registration goes ahead of the component library's, whose state starts in the default choice, so a request's scope draws the request's.</remarks>
    /// <param name="services">The collection to register into.</param>
    /// <param name="givenChoice">The choice the host gives a request, ahead of the store and the browser's record, of which its page keeps nothing.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaThemeServerIntegration(this IServiceCollection services, Func<HttpContext, ThemeChoice?>? givenChoice = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.TryAddSingleton(new ThemeServerOptions { GivenChoice = givenChoice });
        services.TryAddScoped<IThemeState>(provider => new DefaultThemeState(
            provider.GetRequiredService<ThemeRegistry>(),
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext?.GetFormaTheme()?.Choice));

        services.AddFormaThemeCoordination();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, ThemeCircuitHandler>(provider => new ThemeCircuitHandler(provider.GetRequiredService<ThemeCoordinatorRegistration>())));

        return services;
    }
}
