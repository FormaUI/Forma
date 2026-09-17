namespace Forma.Tests.Localization;

using System.Globalization;

using Forma.Framework.Localization;

/// <summary>An in-memory locale for exercising code that consumes the contract.</summary>
/// <param name="translations">The translations to serve, keyed by the English source text.</param>
/// <param name="searchedLocation">The location to report on every result.</param>
public sealed class StubStringLocale(IReadOnlyDictionary<string, string> translations, string? searchedLocation = null) : IStringLocale
{
    /// <summary>The translation for a key, falling back to the key itself.</summary>
    public LocalizedText this[string name]
        => GetText(name, []);

    /// <summary>The translation for a key, formatted with values paired to the key's holes by position.</summary>
    public LocalizedText this[string name, params ReadOnlySpan<object?> values]
        => GetText(name, LocalizedFormat.Bind(name, values));

    /// <summary>The localized format for a key, with every hole left open.</summary>
    public LocalizedPattern Resolve(string name)
    {
        LocalizedArgument[] arguments = [];

        return Resolve(name, arguments);
    }

    /// <summary>The localized format for a key, with values paired to its holes by position.</summary>
    public LocalizedPattern Resolve(string name, params object?[] values)
        => Resolve(name, LocalizedFormat.Bind(name, values));

    /// <summary>Every translation the stub was given.</summary>
    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
    {
        foreach (var entry in translations)
        {
            yield return new LocalizedText(entry.Key, entry.Value, false, searchedLocation);
        }
    }

    private LocalizedPattern Resolve(string name, LocalizedArgument[] arguments)
        => new(name, Find(name, out var found), arguments, !found, searchedLocation);

    private string Find(string name, out bool found)
    {
        found = translations.TryGetValue(name, out var translation);

        return translation ?? name;
    }

    private LocalizedText GetText(string name, LocalizedArgument[] arguments)
    {
        var format = Find(name, out var found);
        var value = arguments.Length == 0 ? format : LocalizedFormat.Render(format, arguments, CultureInfo.CurrentCulture);

        return new LocalizedText(name, value, !found, searchedLocation);
    }
}
