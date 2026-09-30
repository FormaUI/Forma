namespace Forma.Framework.Localization.Json;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Files;

/// <summary>A locale reading the strings for one resource location from JSON locale files.</summary>
internal sealed class JsonStringLocale(JsonLocaleContext context, ResourceLocation location) : IStringLocale
{
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
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var culture = CultureInfo.CurrentUICulture; culture.Name.Length != 0; culture = culture.Parent)
        {
            var file = context.Cache.Read(location, culture);
            if (file is not null)
            {
                foreach (var entry in file)
                {
                    if (seen.Add(entry.Key))
                    {
                        yield return new LocalizedText(entry.Key, entry.Value.Format, ResourceNotFound: false, location.BaseName);
                    }
                }
            }

            if (!includeParentCultures)
            {
                yield break;
            }
        }
    }

    // The key is looked up and falls back as the text it stands for, with its escape removed.
    private LocalizedText Render(string name, LocalizedArgument[] arguments)
    {
        var key = LocalizationSchema.Unescape(name);
        var found = TryFind(key, out var entry);
        RequireFound(name, found);

        var format = Evaluate(found ? entry.Format : key, IsLocalized(found, entry), arguments);

        return new LocalizedText(name, LocalizedFormat.Render(format, arguments, CultureInfo.CurrentCulture), !found, location.BaseName);
    }

    private LocalizedPattern Resolve(string name, LocalizedArgument[] arguments)
    {
        var key = LocalizationSchema.Unescape(name);
        var found = TryFind(key, out var entry);
        RequireFound(name, found);

        return new LocalizedPattern(name, Evaluate(found ? entry.Format : key, IsLocalized(found, entry), arguments), arguments, !found, location.BaseName);
    }

    // A format that is the key itself is source-language text.
    private static bool IsLocalized(bool found, JsonResourceEntry entry)
        => found && !entry.IsSourceText;

    // Plural rules follow the language the text is written in.
    private string Evaluate(string format, bool localized, IReadOnlyList<LocalizedArgument> arguments)
        => context.PluralEvaluator.Evaluate(
            format,
            localized ? CultureInfo.CurrentUICulture : context.SourceCulture,
            CultureInfo.CurrentUICulture,
            arguments);

    private void RequireFound(string name, bool found)
    {
        if (!found && context.Options.RequireMatchingLocalization)
        {
            throw new LocalizationNotFoundException(
                $"'{location.BaseName}' carries no translation of '{name}' for '{CultureInfo.CurrentUICulture.Name}'.");
        }
    }

    private bool TryFind(string name, out JsonResourceEntry entry)
    {
        for (var culture = CultureInfo.CurrentUICulture; culture.Name.Length != 0; culture = culture.Parent)
        {
            var file = context.Cache.Read(location, culture);
            if (file is not null && file.TryGetValue(name, out entry))
            {
                return true;
            }
        }

        entry = default;

        return false;
    }

}
