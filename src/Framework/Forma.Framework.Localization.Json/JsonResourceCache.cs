namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;

using Forma.Framework.Localization.Files;

using Microsoft.Extensions.Logging;

/// <summary>A cache of the entries for a type in one culture, merged from the files embedded in the type's assembly, the locale packs and the host's own files, those embedded in the host's assemblies and then its folder's, a later source winning.</summary>
internal sealed class JsonResourceCache
{
    private readonly ILocaleFileFormat _format;
    private readonly ConcurrentDictionary<Assembly, EmbeddedLocaleSource> _embeddedByAssembly = new();
    private readonly ConcurrentDictionary<string, EmbeddedLocaleSource> _embeddedByAssemblyName = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<CacheKey, FrozenDictionary<string, JsonResourceEntry>> _files = new();
    private readonly ConcurrentDictionary<(LocaleConflict Conflict, string CultureName), byte> _reportedConflicts = new();
    private readonly LocaleSource[] _layered;
    private readonly ILogger _logger;

    /// <summary>Creates a cache reading files in one format.</summary>
    /// <param name="format">The format the files are written in.</param>
    /// <param name="localesPath">The folder the host keeps its own locale files in.</param>
    /// <param name="packs">The locale packs, in the order they are laid over one another.</param>
    /// <param name="hostAssemblies">The assemblies the host's own files are embedded in, in the order they are laid over one another.</param>
    /// <param name="logger">The logger a source's conflicts are reported through.</param>
    public JsonResourceCache(ILocaleFileFormat format, string localesPath, IReadOnlyList<Assembly> packs, IReadOnlyList<Assembly>? hostAssemblies, ILogger logger)
    {
        _format = format;
        _logger = logger;
        _layered = [
            .. packs.Select(pack => new LocalePackSource(pack, format, GetRootNamespace)),
            .. (hostAssemblies ?? []).Select(host => new ApplicationLocaleSource(host, format, GetRootNamespace)),
            new HostLocaleSource(localesPath, format, GetRootNamespace),
        ];
    }

    /// <summary>The entries for a location in one culture.</summary>
    public FrozenDictionary<string, JsonResourceEntry> Read(ResourceLocation location, CultureInfo culture)
        => _files.GetOrAdd(new CacheKey(location, culture.Name), static (key, cache) => cache.Load(key), this);

    /// <summary>Whether a culture or a culture it falls back to holds an entry for the location's type, a shared one among them.</summary>
    public bool HasTypeEntries(ResourceLocation location, CultureInfo culture)
    {
        for (var fallback = culture; fallback.Name.Length != 0; fallback = fallback.Parent)
        {
            if (Read(location, fallback).Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether a culture or a culture it falls back to holds an entry for any type of the location's assembly, in any source.</summary>
    public bool HasAssemblyEntries(ResourceLocation location, CultureInfo culture)
    {
        var embeddedSource = GetEmbeddedSource(location);

        for (var fallback = culture; fallback.Name.Length != 0; fallback = fallback.Parent)
        {
            if (!embeddedSource.ReadEntries(location.AssemblyName, fallback.Name).IsEmpty)
            {
                return true;
            }

            foreach (var source in _layered)
            {
                if (!source.ReadEntries(location.AssemblyName, fallback.Name).IsEmpty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private FrozenDictionary<string, JsonResourceEntry> Load(CacheKey key)
    {
        var (assemblyName, typeName) = (key.Location.AssemblyName, key.Location.TypeName);
        var embeddedSource = GetEmbeddedSource(key.Location);

        var entries = new Dictionary<string, LocaleSourceEntry>(StringComparer.Ordinal);
        var conflicts = new List<LocaleConflict>();
        embeddedSource.ReadEntries(assemblyName, key.CultureName).Collect(typeName, entries, conflicts);
        foreach (var source in _layered)
        {
            source.ReadEntries(assemblyName, key.CultureName).Collect(typeName, entries, conflicts);
        }

        ReportConflicts(conflicts, key.CultureName);

        if (entries.Count == 0)
        {
            return [];
        }

        // A null value says the text is deliberately the key itself.
        return entries.ToFrozenDictionary(
            entry => entry.Key,
            entry => new JsonResourceEntry(entry.Key, entry.Value.Value ?? entry.Key, entry.Value.Value is null),
            StringComparer.Ordinal);
    }

    // The files embedded in the location's assembly, whose config names the root namespace the other sources read the assembly's names by.
    private EmbeddedLocaleSource GetEmbeddedSource(ResourceLocation location)
    {
        var embeddedSource = _embeddedByAssembly.GetOrAdd(location.Assembly, static (owner, format) => new EmbeddedLocaleSource(owner, format), _format);

        // Of two assemblies of one name, the one read last names the root namespace.
        if (!_embeddedByAssemblyName.TryGetValue(location.AssemblyName, out var namedSource) || namedSource != embeddedSource)
        {
            _embeddedByAssemblyName[location.AssemblyName] = embeddedSource;
        }

        return embeddedSource;
    }

    // A conflict is reported once, the first time a type it touches is read, however many types beneath a shared entry meet it.
    private void ReportConflicts(List<LocaleConflict> conflicts, string cultureName)
    {
        foreach (var conflict in conflicts)
        {
            if (_reportedConflicts.TryAdd((conflict, cultureName), 0))
            {
                JsonLocalizationLog.EntryConflict(_logger, conflict.FileName, conflict.OtherFileName, conflict.Key, conflict.TypeName, cultureName);
            }
        }
    }

    // The root namespace an assembly's embedded config declares, which a host's folder and a pack lay its files out by.
    private string? GetRootNamespace(string assemblyName)
        => _embeddedByAssemblyName.TryGetValue(assemblyName, out var embeddedSource) ? embeddedSource.GetRootNamespace(assemblyName) : null;

    private readonly record struct CacheKey(ResourceLocation Location, string CultureName);
}

/// <summary>An entry's text together with the key it was read under.</summary>
internal readonly record struct JsonResourceEntry(string Name, string Format, bool IsSourceText);
