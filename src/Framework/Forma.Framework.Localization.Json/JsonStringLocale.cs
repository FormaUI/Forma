namespace Forma.Framework.Localization.Json;

using System.Collections.Frozen;
using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Files;

/// <summary>A locale reading the strings for one resource location from JSON locale files.</summary>
/// <param name="context">The cache, source language, plural rules and options the factory's locales share.</param>
/// <param name="location">The resource location the strings are read for.</param>
/// <param name="boundCulture">The culture that formats the values, or <see langword="null"/> to read the current one.</param>
/// <param name="boundUICulture">The culture that chooses the strings, or <see langword="null"/> to read the current one.</param>
internal sealed class JsonStringLocale(JsonLocaleContext context, ResourceLocation location, CultureInfo? boundCulture = null, CultureInfo? boundUICulture = null) : IStringLocale
{
    // What a bound locale read for its UI culture and each culture it falls back to, nearest first, each read on first use.
    private readonly FrozenDictionary<string, JsonResourceEntry>?[] _boundEntries = new FrozenDictionary<string, JsonResourceEntry>?[CountCultures(boundUICulture)];

    private CultureInfo Culture
        => boundCulture ?? CultureInfo.CurrentCulture;

    private CultureInfo UICulture
        => boundUICulture ?? CultureInfo.CurrentUICulture;

    /// <inheritdoc />
    public LocalizedText this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return Render(name, []);
        }
    }

    /// <inheritdoc />
    public LocalizedText this[string name, params ReadOnlySpan<object?> values]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return Render(name, LocalizedFormat.Bind(name, values));
        }
    }

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        LocalizedArgument[] arguments = [];

        return Resolve(name, arguments);
    }

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name, params object?[] values)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(values);

        return Resolve(name, LocalizedFormat.Bind(name, values));
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
    {
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        var uiCulture = UICulture;
        var index = 0;
        for (var culture = uiCulture; culture.Name.Length != 0; culture = culture.Parent, index++)
        {
            foreach (var entry in Read(uiCulture, culture, index))
            {
                if (seenKeys.Add(entry.Key))
                {
                    yield return new LocalizedText(entry.Key, entry.Value.Format, ResourceNotFound: false, location.BaseName);
                }
            }

            if (!includeParentCultures)
            {
                yield break;
            }
        }
    }

    /// <inheritdoc />
    public IStringLocale WithCulture(CultureInfo culture, CultureInfo uiCulture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        return new JsonStringLocale(context, location, culture, uiCulture);
    }

    // The key is looked up and falls back as the text it stands for, with its escape removed.
    private LocalizedText Render(string name, LocalizedArgument[] arguments)
    {
        var key = LocalizationSchema.Unescape(name);
        var isFound = Find(name, key, out var entry);

        var format = Evaluate(isFound ? entry.Format : key, IsLocalized(isFound, entry), arguments);

        return new LocalizedText(name, LocalizedFormat.Render(format, arguments, Culture, UICulture), !isFound, location.BaseName);
    }

    private LocalizedPattern Resolve(string name, LocalizedArgument[] arguments)
    {
        var key = LocalizationSchema.Unescape(name);
        var isFound = Find(name, key, out var entry);

        return new LocalizedPattern(name, Evaluate(isFound ? entry.Format : key, IsLocalized(isFound, entry), arguments), arguments, !isFound, location.BaseName);
    }

    // The entry the key resolves to in the UI culture, checked by the diagnostics the options run and required where they require it.
    private bool Find(string name, string key, out JsonResourceEntry entry)
    {
        var uiCulture = UICulture;
        var isFound = TryFind(key, uiCulture, out entry);

        if (context.Diagnostics is { } diagnostics)
        {
            Diagnose(diagnostics, name, key, uiCulture, isFound);
        }

        if (!isFound && context.Options.RequireMatchingLocalization)
        {
            throw new LocalizationNotFoundException(
                $"'{location.BaseName}' carries no localization of '{name}' for '{uiCulture.Name}'.");
        }

        return isFound;
    }

    // The UI culture's own lookup stands for it, and every other culture the diagnostics check resolves the key the same way.
    private void Diagnose(JsonLocaleDiagnostics diagnostics, string name, string key, CultureInfo uiCulture, bool isFound)
    {
        if (!isFound && !context.IsSourceLanguage(uiCulture))
        {
            diagnostics.ReportMissing(location, name, uiCulture);
        }

        foreach (var culture in diagnostics.Cultures)
        {
            if (string.Equals(culture.Name, uiCulture.Name, StringComparison.Ordinal) || context.IsSourceLanguage(culture))
            {
                continue;
            }

            // A file another culture can't read is reported, and leaves the text of the reader's culture as it is.
            try
            {
                if (!TryFind(key, culture, out _))
                {
                    diagnostics.ReportMissing(location, name, culture);
                }
            }
            catch (LocaleFileException exception)
            {
                diagnostics.ReportUnread(location, culture, exception);
            }
        }
    }

    // A format that is the key itself is source-language text.
    private static bool IsLocalized(bool isFound, JsonResourceEntry entry)
        => isFound && !entry.IsSourceText;

    // Plural rules follow the language the text is written in.
    private string Evaluate(string format, bool isLocalized, IReadOnlyList<LocalizedArgument> arguments)
        => context.PluralEvaluator.Evaluate(
            format,
            isLocalized ? UICulture : context.SourceCulture,
            UICulture,
            arguments);

    // The entry a key resolves to in a culture: the culture's own, or the nearest culture it falls back to that holds one.
    private bool TryFind(string key, CultureInfo uiCulture, out JsonResourceEntry entry)
    {
        var index = 0;
        for (var culture = uiCulture; culture.Name.Length != 0; culture = culture.Parent, index++)
        {
            if (Read(uiCulture, culture, index).TryGetValue(key, out entry))
            {
                return true;
            }
        }

        entry = default;

        return false;
    }

    // A bound locale keeps what it read for each culture its UI culture falls back through, so a later lookup doesn't read them again.
    private FrozenDictionary<string, JsonResourceEntry> Read(CultureInfo uiCulture, CultureInfo culture, int index)
        => boundUICulture is null || !ReferenceEquals(uiCulture, boundUICulture)
            ? context.Cache.Read(location, culture)
            : _boundEntries[index] ??= context.Cache.Read(location, culture);

    private static int CountCultures(CultureInfo? uiCulture)
    {
        var count = 0;
        for (var culture = uiCulture; culture is not null && culture.Name.Length != 0; culture = culture.Parent)
        {
            count++;
        }

        return count;
    }
}
