namespace Forma.Framework.Localization;

using System.Globalization;
using System.Reflection;

using Microsoft.Extensions.Localization;

/// <summary>Reads the strings for one base name, walking the culture chain and falling back to the key itself.</summary>
internal sealed class JsonStringLocalizer(JsonResourceCache cache, Assembly assembly, string baseName) : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var translation = Find(name);
            return new LocalizedString(name, translation ?? name, translation is null, baseName);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(arguments);

            var translation = Find(name);
            var formatted = string.Format(CultureInfo.CurrentCulture, translation ?? name, arguments);
            return new LocalizedString(name, formatted, translation is null, baseName);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
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
                        yield return new LocalizedString(entry.Key, entry.Value, resourceNotFound: false, baseName);
                    }
                }
            }

            if (!includeParentCultures || culture.Name.Length == 0)
            {
                yield break;
            }
        }
    }

    private string? Find(string name)
    {
        for (var culture = CultureInfo.CurrentUICulture; ; culture = culture.Parent)
        {
            var file = cache.Read(assembly, baseName, culture);
            if (file is not null && file.TryGetValue(name, out var translation))
            {
                return translation;
            }

            if (culture.Name.Length == 0)
            {
                return null;
            }
        }
    }
}
