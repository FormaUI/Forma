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
        => Text(name, []);

    /// <summary>The translation for a key, formatted with the supplied arguments.</summary>
    public LocalizedText this[string name, params ReadOnlySpan<object?> arguments]
        => Text(name, arguments.ToArray());

    /// <summary>The translation for an interpolated string.</summary>
    public LocalizedText this[LocalizedFormatHandler handler]
    {
        get
        {
            var name = handler.Key.ToString();
            var arguments = handler.GetArguments();
            handler.Clear();

            return Text(name, arguments);
        }
    }

    /// <summary>The translated format for a key, with the holes left open.</summary>
    public LocalizedPattern Resolve(string name, params object?[] arguments)
        => new(name, Find(name, out var found), arguments, !found, searchedLocation);

    /// <summary>The translated format for an interpolated string, with the holes left open.</summary>
    public LocalizedPattern Resolve(LocalizedFormatHandler handler)
    {
        var name = handler.Key.ToString();
        var arguments = handler.GetArguments();
        handler.Clear();

        return new LocalizedPattern(name, Find(name, out var found), arguments, !found, searchedLocation);
    }

    /// <summary>Every translation the stub was given.</summary>
    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
    {
        foreach (var entry in translations)
        {
            yield return new LocalizedText(entry.Key, entry.Value, false, searchedLocation);
        }
    }

    private string Find(string name, out bool found)
    {
        found = translations.TryGetValue(name, out var translation);

        return translation ?? name;
    }

    private LocalizedText Text(string name, object?[] arguments)
    {
        var format = Find(name, out var found);
        var value = arguments.Length == 0 ? format : string.Format(CultureInfo.CurrentCulture, format, arguments);

        return new LocalizedText(name, value, !found, searchedLocation);
    }
}
