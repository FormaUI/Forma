namespace Forma.Tests.Localization;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>Entries held per UI culture, each lookup answered from the entries of the UI culture in force.</summary>
/// <param name="entriesByCulture">The entries of each UI culture, keyed by the culture's name and then by the source text.</param>
public sealed class CultureEntryDictionary(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> entriesByCulture) : IReadOnlyDictionary<string, string>
{
    /// <inheritdoc />
    public int Count
        => CurrentEntries.Count;

    /// <inheritdoc />
    public IEnumerable<string> Keys
        => CurrentEntries.Keys;

    /// <inheritdoc />
    public IEnumerable<string> Values
        => CurrentEntries.Values;

    private IReadOnlyDictionary<string, string> CurrentEntries
        => entriesByCulture.TryGetValue(CultureInfo.CurrentUICulture.Name, out var entries) ? entries : new Dictionary<string, string>();

    /// <inheritdoc />
    public string this[string key]
        => CurrentEntries[key];

    /// <inheritdoc />
    public bool ContainsKey(string key)
        => CurrentEntries.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
        => CurrentEntries.TryGetValue(key, out value);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        => CurrentEntries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
