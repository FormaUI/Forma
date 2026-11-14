namespace Forma.Framework.Localization;

using Forma.Framework.Culture;

/// <summary>Creates locales that look their strings up in a culture state's cultures, and localizes text carried as data.</summary>
public static class StringLocaleFactoryExtensions
{
    /// <summary>The text <paramref name="text"/> names, looked up in the current UI culture and its values formatted in the current culture, each value that is itself localizable text localized in turn.</summary>
    /// <param name="factory">The factory that creates the text's locale.</param>
    /// <param name="text">The text to localize.</param>
    public static LocalizedText Localize(this IStringLocaleFactory factory, LocalizableText text)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(text);

        return Localize(factory, text, locale => locale);
    }

    /// <summary>The text <paramref name="text"/> names, looked up in the culture state's cultures, each value that is itself localizable text localized in turn.</summary>
    /// <param name="factory">The factory that creates the text's locale.</param>
    /// <param name="text">The text to localize.</param>
    /// <param name="cultureState">The culture state whose cultures the text is looked up and formatted in.</param>
    public static LocalizedText Localize(this IStringLocaleFactory factory, LocalizableText text, ICultureState cultureState)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(cultureState);

        return Localize(factory, text, locale => locale.WithCulture(cultureState.Culture, cultureState.UICulture));
    }

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

    // Looks the text up in the locale `bind` gives, localizing each value that is localizable text in that locale's cultures.
    private static LocalizedText Localize(IStringLocaleFactory factory, LocalizableText text, Func<IStringLocale, IStringLocale> bind)
    {
        var locale = bind(factory.Create(text.Source));

        if (text.Arguments.Count == 0)
        {
            return locale[text.Key];
        }

        var values = text.Arguments.Select(argument => argument is LocalizableText inner ? Localize(factory, inner, bind).Value : argument).ToArray();

        return locale[text.Key, values];
    }
}
