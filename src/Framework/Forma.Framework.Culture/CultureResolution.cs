namespace Forma.Framework.Culture;

using System.Globalization;

/// <summary>The culture an application uses for a culture name, its culture provider's where it registered one.</summary>
internal static class CultureResolution
{
    /// <summary>Gets the culture the application uses for <paramref name="name"/>: the provider's, or the platform's where there is no provider.</summary>
    /// <exception cref="InvalidOperationException">The provider returned no culture, or a culture under another name.</exception>
    public static CultureInfo GetCulture(ICultureProvider? cultureProvider, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return cultureProvider is null ? CultureInfo.GetCultureInfo(name) : GetProvidedCulture(cultureProvider, name);
    }

    /// <summary>Gets the culture the application uses for the name of <paramref name="culture"/>: the provider's, or <paramref name="culture"/> itself where there is no provider.</summary>
    /// <exception cref="InvalidOperationException">The provider returned no culture, or a culture under another name.</exception>
    public static CultureInfo GetCulture(ICultureProvider? cultureProvider, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        return cultureProvider is null ? culture : GetProvidedCulture(cultureProvider, culture.Name);
    }

    // The page's language, its culture addresses and its coordinator name a culture by its name, so a culture under another one would set them apart.
    private static CultureInfo GetProvidedCulture(ICultureProvider cultureProvider, string name)
    {
        var culture = cultureProvider.GetCulture(name);

        return culture is not null && string.Equals(culture.Name, name, StringComparison.OrdinalIgnoreCase)
            ? culture
            : throw new InvalidOperationException(
                $"The registered {nameof(ICultureProvider)}, {cultureProvider.GetType().Name}, returned {(culture is null ? "no culture" : $"the culture '{culture.Name}'")} for the culture name '{name}'; a culture provider returns a culture carrying the name it is asked for.");
    }
}
