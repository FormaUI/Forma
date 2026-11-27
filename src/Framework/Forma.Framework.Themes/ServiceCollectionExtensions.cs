namespace Forma.Framework.Themes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the theme system's services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the themes <paramref name="configure"/> sets and the registry they make.</summary>
    /// <remarks>
    /// Every call's <paramref name="configure"/> applies in the order of the calls, after the themes the component library registers
    /// as its own, so a host removes or replaces Forma's themes whichever registration comes first. The theme state each user scope
    /// holds is the runtime's, which its aggregate registrar registers, so a host's call comes before or after it alike.
    /// </remarks>
    /// <param name="services">The collection to register into.</param>
    /// <param name="configure">Adds, removes or replaces themes, or sets the choice a reader starts in.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaThemes(this IServiceCollection services, Action<ThemeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var registrations = GetRegistrations(services);

        if (configure is not null)
        {
            registrations.Configurations.Add(configure);
        }

        services.TryAddSingleton(provider => provider.GetRequiredService<ThemeRegistrations>().CreateRegistry());

        return services;
    }

    /// <summary>Registers the theme state a runtime holds for each of its user scopes, in the lifetime <paramref name="lifetime"/>, and the integration a switch applies an accepted choice through, the same instance.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="lifetime">One state per user scope, or one per process where the runtime is one user scope.</param>
    /// <param name="startingChoice">The choice a state starts in, where the runtime has one of its own, such as a request's.</param>
    /// <returns>The same collection.</returns>
    internal static IServiceCollection AddFormaThemeState(this IServiceCollection services, ServiceLifetime lifetime, Func<IServiceProvider, ThemeChoice?>? startingChoice = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAdd(new ServiceDescriptor(typeof(IThemeState), provider => new DefaultThemeState(provider.GetRequiredService<ThemeRegistry>(), startingChoice?.Invoke(provider)), lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IThemeStateIntegration), provider => provider.GetRequiredService<IThemeState>() as IThemeStateIntegration
            ?? throw new InvalidOperationException($"The registered {nameof(IThemeState)} doesn't implement {nameof(IThemeStateIntegration)}, through which a switch applies the choice it accepted; a state registered in place of Forma's implements both on the same instance."), lifetime));

        return services.AddFormaThemes();
    }

    /// <summary>Registers themes the component library offers as its own, applied before any host's configuration, and a theme state for each user scope where the runtime's registrar registered none.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="configure">Adds the library's themes.</param>
    /// <returns>The same collection.</returns>
    internal static IServiceCollection AddFormaThemeDefaults(this IServiceCollection services, Action<ThemeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var registrations = GetRegistrations(services);

        if (!registrations.Defaults.Contains(configure))
        {
            registrations.Defaults.Add(configure);
        }

        return services.AddFormaThemeState(ServiceLifetime.Scoped);
    }

    // The one record of every call's configuration, kept in the collection so the calls can come in any order.
    private static ThemeRegistrations GetRegistrations(IServiceCollection services)
    {
        if (services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(ThemeRegistrations))?.ImplementationInstance is ThemeRegistrations registrations)
        {
            return registrations;
        }

        registrations = new ThemeRegistrations();
        services.AddSingleton(registrations);

        return registrations;
    }
}
