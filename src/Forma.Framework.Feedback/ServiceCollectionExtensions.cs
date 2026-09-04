namespace Forma.Framework.Feedback;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the feedback services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the toast and confirm dialog services this package ships and an empty status badge catalog, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>The statuses are an application's to define, so the default <see cref="IStatusBadgeCatalog"/> holds none; a badge drawn with its own colors or none at all needs no more.</remarks>
    public static IServiceCollection AddFormaFeedbackServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Scoped, because each circuit holds its own messages and pending confirmations.
        services.TryAddScoped<IToastService, ToastService>();
        services.TryAddScoped<IConfirmDialogService, ConfirmDialogService>();
        services.TryAddSingleton<IStatusBadgeCatalog>(new StatusBadgeCatalog());

        return services;
    }
}
