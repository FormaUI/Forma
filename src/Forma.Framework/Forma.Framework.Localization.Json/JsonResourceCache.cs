namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;

using Forma.Framework.Localization.Files;

/// <summary>Reads the translations for a type in one culture, the files embedded in its assembly with the host's own laid over them, and keeps each result in memory after the first read.</summary>
/// <param name="format">The format the files are written in.</param>
/// <param name="host">The folder the host keeps its own locale files in.</param>
internal sealed class JsonResourceCache(ILocaleFileFormat format, HostLocaleSource host)
{
    private readonly ILocaleFileFormat _format = format;
    private readonly HostLocaleSource _host = host;
    private readonly ConcurrentDictionary<Assembly, EmbeddedLocaleSource> _embedded = new();
    private readonly ConcurrentDictionary<CacheKey, FrozenDictionary<string, JsonResourceEntry>?> _files = new();

    /// <summary>The translations for one culture, or null when neither the assembly nor the host carries any for it.</summary>
    public FrozenDictionary<string, JsonResourceEntry>? Read(ResourceLocation location, CultureInfo culture)
        => _files.GetOrAdd(new CacheKey(location, culture.Name), Load);

    private FrozenDictionary<string, JsonResourceEntry>? Load(CacheKey key)
    {
        var (assembly, _, typeName) = key.Location;
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        var embedded = _embedded.GetOrAdd(assembly, static (owner, format) => new EmbeddedLocaleSource(owner, format), _format);

        var entries = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var entry in embedded.Read(assemblyName, typeName, key.CultureName).Concat(_host.Read(assemblyName, typeName, key.CultureName)))
        {
            entries[entry.Key] = entry.Value;
        }

        if (entries.Count == 0)
        {
            return null;
        }

        // A null value says the translation is deliberately the key itself, so nothing downstream ever sees one.
        return entries.ToFrozenDictionary(
            entry => entry.Key,
            entry => new JsonResourceEntry(entry.Key, entry.Value ?? entry.Key, entry.Value is null),
            StringComparer.Ordinal);
    }

    private readonly record struct CacheKey(ResourceLocation Location, string CultureName);
}

/// <summary>A translation together with the key it was read under.</summary>
internal readonly record struct JsonResourceEntry(string Name, string Format, bool IsSourceText);
