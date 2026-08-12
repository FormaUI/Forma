namespace Forma.Framework.Localization;

using Microsoft.Extensions.Localization;

/// <summary>Presents an <see cref="IStringLocale"/> as the framework's <see cref="IStringLocalizer"/>.</summary>
/// <param name="locale">The locale the translations come from.</param>
public sealed class StringLocalizerAdapter(IStringLocale locale) : IStringLocalizer
{
    private readonly IStringLocale _locale = locale ?? throw new ArgumentNullException(nameof(locale));

    /// <summary>The translation for a key, falling back to the key itself.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    public LocalizedString this[string name]
        => Convert(_locale[name]);

    /// <summary>The translation for a key, formatted with the supplied arguments.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    /// <param name="arguments">The values the holes name.</param>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(arguments);

            return Convert(_locale[name, arguments]);
        }
    }

    /// <summary>Every translation available for the current culture.</summary>
    /// <param name="includeParentCultures">Whether to include translations inherited from parent cultures.</param>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        foreach (var text in _locale.GetAllStrings(includeParentCultures))
        {
            yield return Convert(text);
        }
    }

    private static LocalizedString Convert(LocalizedText text)
        => new(text.Name, text.Value, text.ResourceNotFound, text.SearchedLocation);
}
