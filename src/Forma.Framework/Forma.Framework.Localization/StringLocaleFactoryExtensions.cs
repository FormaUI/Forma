namespace Forma.Framework.Localization;

using Forma.Framework.Globalization;

/// <summary>Creates locales that look their strings up in a culture state's cultures.</summary>
public static class StringLocaleFactoryExtensions
{
    /// <summary>A locale for the resource that belongs to a type, looking its strings up in the culture state's cultures as they change.</summary>
    /// <param name="factory">The factory that creates the locale.</param>
    /// <param name="resourceSource">The type whose resource the strings belong to.</param>
    /// <param name="cultureState">The culture state whose cultures the strings are looked up in.</param>
    /// <returns>A locale bound to the state's cultures, and bound again on its first use after each change of the state.</returns>
    public static IStringLocale Create(this IStringLocaleFactory factory, Type resourceSource, ICultureState cultureState)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(resourceSource);
        ArgumentNullException.ThrowIfNull(cultureState);

        return new CultureStateLocale(factory.Create(resourceSource), cultureState);
    }
}
