namespace Forma.Framework.Localization;

using System.Globalization;

/// <summary>Provides localized strings for one resource base name.</summary>
public interface IStringLocale
{
    /// <summary>The localized text for a key.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    LocalizedText this[string name] { get; }

    /// <summary>The localized text for a key, its holes filled with the values.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    /// <param name="values">One value per distinct hole, in the order the holes first appear in the key.</param>
    /// <remarks>
    /// The values pair with the key's holes by position. A key naming no hole takes a single value, which an
    /// expression written without a name reads.
    /// </remarks>
    LocalizedText this[string name, params ReadOnlySpan<object?> values] { get; }

    /// <summary>The localized format for a key, with plural forms resolved and every hole left open.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    LocalizedPattern Resolve(string name);

    /// <summary>The localized format for a key, with values paired to its holes by position.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    /// <param name="values">One value per distinct hole, in the order the holes first appear in the key.</param>
    LocalizedPattern Resolve(string name, params object?[] values);

    /// <summary>Every translation available for the UI culture the locale looks its strings up in.</summary>
    /// <param name="includeParentCultures">Whether to include translations inherited from parent cultures.</param>
    IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures);

    /// <summary>Returns a locale that uses the specified formatting and UI cultures, sharing the underlying localization resources. The original locale is unchanged.</summary>
    /// <param name="culture">The culture that formats the values filling a string's holes.</param>
    /// <param name="uiCulture">The culture that chooses the strings, their fallback and their plural forms.</param>
    /// <returns>A locale bound to both cultures, which replace any the original was bound to.</returns>
    IStringLocale WithCulture(CultureInfo culture, CultureInfo uiCulture);
}
