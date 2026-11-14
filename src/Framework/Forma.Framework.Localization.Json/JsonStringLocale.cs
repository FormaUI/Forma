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

        var index = 0;
        for (var culture = UICulture; culture.Name.Length != 0; culture = culture.Parent, index++)
        {
            foreach (var entry in Read(culture, index))
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
        var isFound = TryFind(key, out var entry);
        RequireFound(name, isFound);

        var format = Evaluate(isFound ? entry.Format : key, IsLocalized(isFound, entry), arguments);

        return new LocalizedText(name, LocalizedFormat.Render(format, arguments, Culture, UICulture), !isFound, location.BaseName);
    }

    private LocalizedPattern Resolve(string name, LocalizedArgument[] arguments)
    {
        var key = LocalizationSchema.Unescape(name);
        var isFound = TryFind(key, out var entry);
        RequireFound(name, isFound);

        return new LocalizedPattern(name, Evaluate(isFound ? entry.Format : key, IsLocalized(isFound, entry), arguments), arguments, !isFound, location.BaseName);
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

    private void RequireFound(string name, bool isFound)
    {
        if (!isFound && context.Options.RequireMatchingLocalization)
        {
            throw new LocalizationNotFoundException(
                $"'{location.BaseName}' carries no localization of '{name}' for '{UICulture.Name}'.");
        }
    }

    private bool TryFind(string name, out JsonResourceEntry entry)
    {
        var index = 0;
        for (var culture = UICulture; culture.Name.Length != 0; culture = culture.Parent, index++)
        {
            if (Read(culture, index).TryGetValue(name, out entry))
            {
                return true;
            }
        }

        entry = default;

        return false;
    }

    // A bound locale keeps what it read for each of its cultures, so a later lookup reads none of them again.
    private FrozenDictionary<string, JsonResourceEntry> Read(CultureInfo culture, int index)
        => boundUICulture is null
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
