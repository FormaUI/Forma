namespace Forma.Framework.Localization.Json;

using Forma.Framework.Localization;

/// <summary>Reads the strings belonging to <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type whose locale files hold the strings.</typeparam>
public sealed class JsonStringLocale<T> : IStringLocale<T>
{
    private readonly IStringLocale _locale;

    /// <summary>Creates a locale for <typeparamref name="T"/> from the given factory.</summary>
    public JsonStringLocale(IStringLocaleFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _locale = factory.Create(typeof(T));
    }

    /// <summary>The translation for a key, falling back to the key itself.</summary>
    public LocalizedText this[string name]
        => _locale[name];

    /// <summary>The translation for a key, formatted with values paired to the key's holes by position.</summary>
    public LocalizedText this[string name, params ReadOnlySpan<object?> values]
        => _locale[name, values];

    /// <summary>The localized format for a key, with every hole left open.</summary>
    public LocalizedPattern Resolve(string name)
        => _locale.Resolve(name);

    /// <summary>The localized format for a key, with values paired to its holes by position.</summary>
    public LocalizedPattern Resolve(string name, params object?[] values)
        => _locale.Resolve(name, values);

    /// <summary>Every string reachable for the current culture.</summary>
    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
        => _locale.GetAllStrings(includeParentCultures);
}
