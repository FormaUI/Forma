namespace Forma.Framework.Culture.Blazor.WebView;

using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

/// <summary>Registers the culture services a Blazor Hybrid application needs.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the application's culture state, its culture coordinator, which orders every switch across the web views and applies each accepted pair to the state, the process's defaults and every web view, the application's switch, and each web view's registration with the coordinator, which its first component starts.</summary>
    /// <remarks>
    /// The state, the coordinator and the switch are the application's, one for every web view; each web view's registrations are
    /// its scope's, released as the web view is disposed. A switch persists the pair in the state alone, and culture routing switches
    /// in place: a cookie policy or a full-load switch is rejected with <see cref="InvalidOperationException"/> as the coordinator is
    /// first resolved. A host that registers its own culture state registers it once for the application, as a singleton.
    /// </remarks>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaCultureWebView(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Ahead of the neutral registrations, which then keep these.
        services.TryAddSingleton<ICultureState>(_ => new DefaultCultureState());
        services.TryAddSingleton(provider => new ApplicationCultureCoordinator(
            provider.GetRequiredService<ICultureState>(),
            provider.GetRequiredService<CultureSwitchPolicy>(),
            provider.GetService<ICultureAddressing>()));
        services.TryAddSingleton<ICultureStateIntegration>(provider => provider.GetRequiredService<ApplicationCultureCoordinator>());
        services.TryAddScoped(provider => new CultureCoordinatorRegistration(
            provider.GetRequiredService<ApplicationCultureCoordinator>().DocumentIntegration,
            provider.GetJSRuntime(),
            provider.GetRequiredService<ILogger<CultureCoordinatorRegistration>>()));
        services.AddFormaCultureSwitchPolicy();
        services.AddFormaCultureCoordination();
        services.TryAddSingleton<ICultureSwitch>(provider => new ApplicationCultureSwitch(provider.GetRequiredService<ApplicationCultureCoordinator>(), provider.GetService<ICultureAddressing>()));
        services.TryAddScoped<ICultureScopeStart>(provider => new WebViewCultureRegistration(
            provider.GetRequiredService<ApplicationCultureCoordinator>(),
            provider.GetRequiredService<CultureCoordinatorRegistration>(),
            provider.GetRequiredService<ICultureState>(),
            provider.GetRequiredService<CultureSwitchPolicy>(),
            provider.GetService<ICultureAddressing>(),
            provider.GetRequiredService<NavigationManager>(),
            provider.GetService<TimeProvider>() ?? TimeProvider.System,
            provider.GetRequiredService<ILogger<WebViewCultureRegistration>>()));

        return services;
    }
}
