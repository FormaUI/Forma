namespace Forma.Tests.Localization;

using System.Globalization;

using Forma.Framework.Culture;
using Forma.Framework.Localization;

/// <summary>A string locale over entries held in memory.</summary>
/// <param name="entries">The entries to serve, keyed by the English source text.</param>
/// <param name="searchedLocation">The searched location every result reports.</param>
/// <param name="boundCulture">The culture that formats the values, or <see langword="null"/> to read the current one.</param>
/// <param name="boundUICulture">The UI culture in force while a value formats itself, or <see langword="null"/> to read the current one.</param>
public sealed class StubStringLocale(IReadOnlyDictionary<string, string> entries, string? searchedLocation = null, CultureInfo? boundCulture = null, CultureInfo? boundUICulture = null) : IStringLocale
{
    /// <inheritdoc />
    public LocalizedText this[string name]
        => GetText(name, []);

    /// <inheritdoc />
    public LocalizedText this[string name, params ReadOnlySpan<object?> values]
        => GetText(name, LocalizedFormat.Bind(name, values));

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name)
    {
        LocalizedArgument[] arguments = [];

        return Resolve(name, arguments);
    }

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name, params object?[] values)
        => Resolve(name, LocalizedFormat.Bind(name, values));

    /// <inheritdoc />
    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
    {
        foreach (var entry in entries)
        {
            yield return new LocalizedText(entry.Key, entry.Value, false, searchedLocation);
        }
    }

    /// <inheritdoc />
    public IStringLocale WithCulture(CultureInfo culture, CultureInfo uiCulture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        return new StubStringLocale(entries, searchedLocation, culture, uiCulture);
    }

    private LocalizedPattern Resolve(string name, LocalizedArgument[] arguments)
        => new(name, Find(name, out var isFound), arguments, !isFound, searchedLocation);

    // Entries kept by culture read the culture in force, so a bound locale looks its text up in its own cultures.
    private string Find(string name, out bool isFound)
    {
        (isFound, var text) = boundUICulture is null
            ? Look(name)
            : AmbientCulture.Run(boundCulture ?? boundUICulture, boundUICulture, () => Look(name));

        return text ?? name;
    }

    private (bool IsFound, string? Text) Look(string name)
        => (entries.TryGetValue(name, out var text), text);

    private LocalizedText GetText(string name, LocalizedArgument[] arguments)
    {
        var format = Find(name, out var isFound);
        var value = arguments.Length == 0 ? format : LocalizedFormat.Render(format, arguments, boundCulture ?? CultureInfo.CurrentCulture, boundUICulture ?? CultureInfo.CurrentUICulture);

        return new LocalizedText(name, value, !isFound, searchedLocation);
    }
}
