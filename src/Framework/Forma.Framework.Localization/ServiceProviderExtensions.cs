namespace Forma.Framework.Localization;

/// <summary>Resolves the string locale factory a localization package registers.</summary>
public static class ServiceProviderExtensions
{
    /// <summary>Gets the string locale factory registered in <paramref name="services"/>, or <see cref="NullStringLocaleFactory.Instance"/>, whose strings are their source text, in a host that registers none.</summary>
    /// <param name="services">The services to resolve from.</param>
    /// <returns>The string locale factory.</returns>
    public static IStringLocaleFactory GetStringLocaleFactory(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.GetService(typeof(IStringLocaleFactory)) as IStringLocaleFactory ?? NullStringLocaleFactory.Instance;
    }
}
