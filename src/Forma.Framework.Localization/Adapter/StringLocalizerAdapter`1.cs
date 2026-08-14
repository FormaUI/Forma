namespace Forma.Framework.Localization.Adapter;

using Forma.Framework.Localization;

using Microsoft.Extensions.Localization;

/// <summary>Presents an <see cref="IStringLocale{T}"/> as the framework's <see cref="IStringLocalizer{T}"/>.</summary>
/// <typeparam name="T">The type whose name and namespace locate the resource.</typeparam>
/// <param name="locale">The locale the translations come from.</param>
public sealed class StringLocalizerAdapter<T>(IStringLocale<T> locale) : IStringLocalizer<T>
{
    private readonly StringLocalizerAdapter _adapter = new(locale);

    /// <summary>The translation for a key, falling back to the key itself.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    public LocalizedString this[string name]
        => _adapter[name];

    /// <summary>The translation for a key, formatted with the supplied arguments.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    /// <param name="arguments">The values the holes name.</param>
    public LocalizedString this[string name, params object[] arguments]
        => _adapter[name, arguments];

    /// <summary>Every translation available for the current culture.</summary>
    /// <param name="includeParentCultures">Whether to include translations inherited from parent cultures.</param>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _adapter.GetAllStrings(includeParentCultures);
}
