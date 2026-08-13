namespace Forma.Framework.Localization;

using System.Globalization;
using System.Reflection;

using Forma.Framework.Localization.Parser;

/// <summary>Reads the strings for one base name, walking the culture chain and falling back to the key itself.</summary>
internal sealed class JsonStringLocale(JsonResourceCache cache, Assembly assembly, string baseName, PluralExpressionEvaluator plural, CultureInfo primaryCulture) : IStringLocale
{
    public LocalizedText this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var found = TryFind(name, out var entry);

            return new LocalizedText(name, found ? entry.Format : name, !found, baseName);
        }
    }

    public LocalizedText this[string name, params ReadOnlySpan<object?> arguments]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var found = TryFind(name, out var entry);
            var retained = arguments.IsEmpty ? [] : arguments.ToArray();
            var format = Evaluate(found ? entry.Format : name, found, retained);

            return new LocalizedText(name, string.Format(CultureInfo.CurrentCulture, format, retained), !found, baseName);
        }
    }

    public LocalizedText this[LocalizedFormatHandler handler]
    {
        get
        {
            var key = LocaleExpressionParser.TryNormalize(handler.Key, out var canonical) ? canonical.AsSpan() : handler.Key;

            var found = TryFindBySpan(key, out var entry);
            var name = found ? entry.Name : canonical ?? key.ToString();
            var arguments = handler.GetArguments();
            handler.Clear();

            var format = Evaluate(found ? entry.Format : name, found, arguments);

            return new LocalizedText(name, string.Format(CultureInfo.CurrentCulture, format, arguments), !found, baseName);
        }
    }

    public LocalizedPattern Resolve(string name, params object?[] arguments)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(arguments);

        var found = TryFind(name, out var entry);

        return new LocalizedPattern(name, Evaluate(found ? entry.Format : name, found, arguments), arguments, !found, baseName);
    }

    public LocalizedPattern Resolve(LocalizedFormatHandler handler)
    {
        var key = LocaleExpressionParser.TryNormalize(handler.Key, out var canonical) ? canonical.AsSpan() : handler.Key;

        var found = TryFindBySpan(key, out var entry);
        var name = found ? entry.Name : canonical ?? key.ToString();
        var arguments = handler.GetArguments();
        handler.Clear();

        return new LocalizedPattern(name, Evaluate(found ? entry.Format : name, found, arguments), arguments, !found, baseName);
    }

    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var culture = CultureInfo.CurrentUICulture; ; culture = culture.Parent)
        {
            var file = cache.Read(assembly, baseName, culture);
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

            if (!includeParentCultures || culture.Name.Length == 0)
            {
                yield break;
            }
        }
    }

    // A miss shows the key, which is written in the primary language, so its plural forms follow that language.
    private string Evaluate(string format, bool found, IReadOnlyList<object?> arguments)
        => plural.Evaluate(format, found ? CultureInfo.CurrentUICulture : primaryCulture, arguments);

    private bool TryFind(string name, out JsonResourceEntry entry)
    {
        for (var culture = CultureInfo.CurrentUICulture; ; culture = culture.Parent)
        {
            var file = cache.Read(assembly, baseName, culture);
            if (file is not null && file.TryGetValue(name, out entry))
            {
                return true;
            }

            if (culture.Name.Length == 0)
            {
                entry = default;

                return false;
            }
        }
    }

    private bool TryFindBySpan(ReadOnlySpan<char> name, out JsonResourceEntry entry)
    {
        for (var culture = CultureInfo.CurrentUICulture; ; culture = culture.Parent)
        {
            var file = cache.Read(assembly, baseName, culture);
            if (file is not null && file.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out entry))
            {
                return true;
            }

            if (culture.Name.Length == 0)
            {
                entry = default;

                return false;
            }
        }
    }
}
