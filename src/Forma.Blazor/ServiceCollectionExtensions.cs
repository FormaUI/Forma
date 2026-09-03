namespace Forma.Blazor;

using Forma.Blazor.Features.Emojis;
using Forma.Features.Feedback;
using Forma.Features.Sanitization;
using Forma.Framework.Timing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        services.AddFormaTimingServices();
        services.AddFormaFeedbackServices();
        services.AddFormaSanitizationServices();

        // Scoped, because the sheets it has adopted are the ones in this circuit's own document.
        services.TryAddScoped<EmojiSpriteLoader>();

        return services;
    }
}
