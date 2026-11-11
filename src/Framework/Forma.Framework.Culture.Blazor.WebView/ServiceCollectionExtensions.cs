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
    /// <summary>Registers, under coordinated web views, the application's culture state, its culture coordinator, which orders every switch across the web views and applies each accepted pair to the state, the process's defaults and every web view, the application's switch, and each web view's registration with the coordinator, which its first component starts; or, under a culture per web view, each web view's own culture state, its switch, and its registration with its own document's coordinator, which its first component starts.</summary>
    /// <remarks>
    /// Under coordinated web views the state, the coordinator and the switch are the application's, one for every web view, and a host
    /// that registers its own culture state registers it once for the application, as a singleton; under a culture per web view each is
    /// the web view's scope's, and a host's own state is registered scoped, since one two web views share is rejected with
    /// <see cref="InvalidOperationException"/> as the second starts. Each web view's registrations are its scope's, released as the web
    /// view is disposed. A switch persists the pair in the state alone, and culture routing switches in place: a cookie policy or a
    /// full-load switch is rejected with <see cref="InvalidOperationException"/> as the coordinator, or a web view's start, is first
    /// resolved.
    /// </remarks>
    /// <param name="services">The collection to register into.</param>
    /// <param name="arrangement">Whether every web view follows the application's culture, or each holds a culture of its own.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaCultureWebViewIntegration(this IServiceCollection services, CultureArrangement arrangement = CultureArrangement.Coordinated)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!Enum.IsDefined(arrangement))
        {
            throw new ArgumentOutOfRangeException(nameof(arrangement), arrangement, "The arrangement is neither coordinated nor independent.");
        }

        if (arrangement is CultureArrangement.Independent)
        {
            services.AddFormaCultureSwitchPolicy();
            services.AddFormaCultureCoordination();
            services.TryAddScoped<ICultureSwitch>(provider => new CoordinatedCultureSwitch(provider.GetRequiredService<CultureCoordinatorRegistration>(), provider.GetService<ICultureAddressing>()));
            services.TryAddScoped<ICultureScopeStart>(provider => new WebViewCultureStart(
                provider.GetRequiredService<CultureCoordinatorRegistration>(),
                provider.GetRequiredService<ICultureState>(),
                provider.GetRequiredService<CultureSwitchPolicy>(),
                provider.GetService<ICultureAddressing>(),
                provider.GetRequiredService<NavigationManager>()));

            return services;
        }

        // Ahead of the neutral registrations, which then keep these.
        services.TryAddSingleton<ICultureState>(provider => new DefaultCultureState(provider.GetService<ICultureProvider>()));
        services.TryAddSingleton(provider => new ApplicationCultureCoordinator(
            provider.GetRequiredService<ICultureState>(),
            provider.GetRequiredService<CultureSwitchPolicy>(),
            provider.GetService<ICultureAddressing>()));
        services.TryAddSingleton<ICultureStateIntegration>(provider => provider.GetRequiredService<ApplicationCultureCoordinator>());
        services.TryAddScoped(provider => new CultureCoordinatorRegistration(
            provider.GetRequiredService<ApplicationCultureCoordinator>().DocumentIntegration,
            provider.GetJSRuntime(),
            provider.GetRequiredService<ILogger<CultureCoordinatorRegistration>>(),
            provider.GetService<ICultureProvider>()));
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
