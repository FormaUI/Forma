namespace Forma.UI.Features.Feedback;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the feedback services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the toast and confirm dialog services this package ships, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>An <see cref="IStatusBadgeCatalog"/> is not registered, because the statuses are an application's to define.</remarks>
    public static IServiceCollection AddFormaFeedbackServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Scoped, because each circuit holds its own messages and pending confirmations.
        services.TryAddScoped<IToastService, ToastService>();
        services.TryAddScoped<IConfirmDialogService, ConfirmDialogService>();

        return services;
    }
}
