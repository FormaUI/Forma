namespace Forma.Framework.Feedback;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the feedback services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the toast and confirm dialog services this package ships and an empty status badge catalog.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaFeedbackServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IToastService, ToastService>();
        services.TryAddScoped<IConfirmDialogService, ConfirmDialogService>();
        services.TryAddSingleton<IStatusBadgeCatalog>(new StatusBadgeCatalog());

        return services;
    }
}
