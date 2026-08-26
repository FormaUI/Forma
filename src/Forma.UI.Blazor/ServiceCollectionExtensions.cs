namespace Forma.UI.Blazor;

using Forma.UI.Features.Feedback;
using Forma.UI.Features.Sanitization;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the services Forma's components need to function.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers every default the components rely on, leaving in place whatever a host registered first.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>Artwork is not registered here; an icon or emoji pack is a package a host adds and calls for itself.</remarks>
    public static IServiceCollection AddFormaUI(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaFeedbackServices();
        services.AddFormaSanitizationServices();

        return services;
    }
}
