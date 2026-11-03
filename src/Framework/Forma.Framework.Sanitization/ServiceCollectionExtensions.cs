namespace Forma.Framework.Sanitization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the sanitizer and markup policy.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the allowlist sanitizer and the rich content policy it applies.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaSanitizationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMarkupPolicy>(RichContentMarkupPolicy.Default);
        services.TryAddSingleton<IHtmlSanitizer, AllowlistHtmlSanitizer>();

        return services;
    }
}
