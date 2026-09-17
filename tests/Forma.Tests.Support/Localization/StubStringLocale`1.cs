namespace Forma.Tests.Localization;

using Forma.Framework.Localization;

/// <summary>An in-memory locale for the resource base name derived from <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type the locale stands in for.</typeparam>
/// <param name="translations">The translations to serve, keyed by the English source text.</param>
public sealed class StubStringLocale<T>(IReadOnlyDictionary<string, string> translations) : IStringLocale<T>
{
    private readonly StubStringLocale _locale = new(translations);

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

    /// <summary>Every translation the stub was given.</summary>
    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
        => _locale.GetAllStrings(includeParentCultures);
}
