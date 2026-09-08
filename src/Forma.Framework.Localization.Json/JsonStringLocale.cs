namespace Forma.Framework.Localization.Json;

using System.Globalization;
using System.Reflection;

using Forma.Framework.Localization;

/// <summary>Reads the strings for one base name, walking the culture chain and falling back to the key itself.</summary>
internal sealed class JsonStringLocale(JsonLocaleContext context, Assembly assembly, string baseName) : IStringLocale
{
    public LocalizedText this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return Render(name, []);
        }
    }

    public LocalizedText this[string name, params ReadOnlySpan<object?> values]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return Render(name, LocalizedFormat.Bind(name, values));
        }
    }

    public LocalizedPattern Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        LocalizedArgument[] arguments = [];

        return Resolve(name, arguments);
    }

    public LocalizedPattern Resolve(string name, params object?[] values)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(values);

        return Resolve(name, LocalizedFormat.Bind(name, values));
    }

    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var culture = CultureInfo.CurrentUICulture; culture.Name.Length != 0; culture = culture.Parent)
        {
            var file = context.Cache.Read(assembly, baseName, culture);
            if (file is not null)
            {
                foreach (var entry in file)
                {
                    if (seen.Add(entry.Key))
                    {
                        yield return new LocalizedText(entry.Key, entry.Value.Format, ResourceNotFound: false, baseName);
                    }
                }
            }

            if (!includeParentCultures)
            {
                yield break;
            }
        }
    }

    private LocalizedText Render(string name, LocalizedArgument[] arguments)
    {
        var found = TryFind(name, out var entry);
        RequireFound(name, found);

        var format = Evaluate(found ? entry.Format : name, IsTranslated(found, entry), arguments);

        return new LocalizedText(name, LocalizedFormat.Render(format, arguments, CultureInfo.CurrentCulture), !found, baseName);
    }

    private LocalizedPattern Resolve(string name, LocalizedArgument[] arguments)
    {
        var found = TryFind(name, out var entry);
        RequireFound(name, found);

        return new LocalizedPattern(name, Evaluate(found ? entry.Format : name, IsTranslated(found, entry), arguments), arguments, !found, baseName);
    }

    // A format that is the key is source-language text, whether it was written as null or never found at all.
    private static bool IsTranslated(bool found, JsonResourceEntry entry)
        => found && !entry.IsSourceText;

    // The text a plural expression inflects decides the rules, so source-language text follows the source language.
    private string Evaluate(string format, bool translated, IReadOnlyList<LocalizedArgument> arguments)
        => context.PluralEvaluator.Evaluate(
            format,
            translated ? CultureInfo.CurrentUICulture : context.SourceCulture,
            CultureInfo.CurrentUICulture,
            arguments);

    // Listing every string stays a survey rather than a demand, so it reports a miss instead of refusing it.
    private void RequireFound(string name, bool found)
    {
        if (!found && context.Options.RequireMatchingLocalization)
        {
            throw new LocalizationNotFoundException(
                $"'{baseName}' carries no translation of '{name}' for '{CultureInfo.CurrentUICulture.Name}'.");
        }
    }

    private bool TryFind(string name, out JsonResourceEntry entry)
    {
        for (var culture = CultureInfo.CurrentUICulture; culture.Name.Length != 0; culture = culture.Parent)
        {
            var file = context.Cache.Read(assembly, baseName, culture);
            if (file is not null && file.TryGetValue(name, out entry))
            {
                return true;
            }
        }

        entry = default;

        return false;
    }

}
