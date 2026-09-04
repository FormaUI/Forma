namespace Forma.Framework.Sanitization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the sanitizer and markup policy this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the allowlist sanitizer and the rich content policy it applies, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>The two register separately, so a host narrowing what its documents may carry replaces the policy and keeps the sanitizer.</remarks>
    public static IServiceCollection AddFormaSanitizationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Singleton, because neither holds state beyond the allowlists they are built from.
        services.TryAddSingleton<IMarkupPolicy>(RichContentMarkupPolicy.Default);
        services.TryAddSingleton<IHtmlSanitizer, AllowlistHtmlSanitizer>();

        return services;
    }
}
