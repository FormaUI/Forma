namespace Forma.Tests.Localization;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>Translations held per UI culture, each lookup answered from the translations of the UI culture in force.</summary>
/// <param name="translationsByCulture">The translations of each UI culture, keyed by the culture's name and then by the English source text.</param>
public sealed class CultureTranslationDictionary(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> translationsByCulture) : IReadOnlyDictionary<string, string>
{
    /// <inheritdoc />
    public int Count
        => CurrentTranslations.Count;

    /// <inheritdoc />
    public IEnumerable<string> Keys
        => CurrentTranslations.Keys;

    /// <inheritdoc />
    public IEnumerable<string> Values
        => CurrentTranslations.Values;

    private IReadOnlyDictionary<string, string> CurrentTranslations
        => translationsByCulture.TryGetValue(CultureInfo.CurrentUICulture.Name, out var translations) ? translations : new Dictionary<string, string>();

    /// <inheritdoc />
    public string this[string key]
        => CurrentTranslations[key];

    /// <inheritdoc />
    public bool ContainsKey(string key)
        => CurrentTranslations.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
        => CurrentTranslations.TryGetValue(key, out value);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        => CurrentTranslations.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
