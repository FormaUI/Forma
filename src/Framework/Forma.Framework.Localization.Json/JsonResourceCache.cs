namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;

using Forma.Framework.Localization.Files;

using Microsoft.Extensions.Logging;

/// <summary>A cache of the translations for a type in one culture, merged from the files embedded in the type's assembly, the locale packs and the host's own files, those embedded in the host's assemblies and then its folder's, a later source winning.</summary>
internal sealed class JsonResourceCache
{
    private readonly ILocaleFileFormat _format;
    private readonly ConcurrentDictionary<Assembly, EmbeddedLocaleSource> _embeddedByAssembly = new();
    private readonly ConcurrentDictionary<string, EmbeddedLocaleSource> _embeddedByAssemblyName = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<CacheKey, FrozenDictionary<string, JsonResourceEntry>?> _files = new();
    private readonly ConcurrentDictionary<LocaleTranslations, byte> _reportedTranslations = new(ReferenceEqualityComparer.Instance);
    private readonly LocalePackSource[] _packs;
    private readonly LocaleSource[] _host;
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
        _packs = [.. packs.Select(pack => new LocalePackSource(pack, format, GetRootNamespace))];
        _host = [
            .. (hostAssemblies ?? []).Select(host => new LocalePackSource(host, format, GetRootNamespace, LocalePackSource.HostResourcePrefix)),
            new HostLocaleSource(localesPath, format, GetRootNamespace),
        ];
    }

    /// <summary>The translations for a location in one culture.</summary>
    public FrozenDictionary<string, JsonResourceEntry>? Read(ResourceLocation location, CultureInfo culture)
        => _files.GetOrAdd(new CacheKey(location, culture.Name), Load);

    private FrozenDictionary<string, JsonResourceEntry>? Load(CacheKey key)
    {
        var (assembly, _, typeName) = key.Location;
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        var embeddedSource = _embeddedByAssembly.GetOrAdd(assembly, static (owner, format) => new EmbeddedLocaleSource(owner, format), _format);
        _embeddedByAssemblyName[assemblyName] = embeddedSource;

        var entries = new Dictionary<string, LocaleSourceEntry>(StringComparer.Ordinal);
        foreach (var source in (LocaleSource[])[embeddedSource, .. _packs, .. _host])
        {
            var translations = source.ReadTranslations(assemblyName, key.CultureName);
            ReportConflicts(translations, key.CultureName);
            translations.Collect(typeName, entries);
        }

        if (entries.Count == 0)
        {
            return null;
        }

        // A null value is a translation that is deliberately the key itself.
        return entries.ToFrozenDictionary(
            entry => entry.Key,
            entry => new JsonResourceEntry(entry.Key, entry.Value.Value ?? entry.Key, entry.Value.Value is null),
            StringComparer.Ordinal);
    }

    // A source's conflicts are reported once, the first time its translations are read for any type.
    private void ReportConflicts(LocaleTranslations translations, string cultureName)
    {
        if (translations.Conflicts.Count == 0 || !_reportedTranslations.TryAdd(translations, 0))
        {
            return;
        }

        foreach (var conflict in translations.Conflicts)
        {
            JsonLocalizationLog.TranslationConflict(_logger, conflict.FileName, conflict.OtherFileName, conflict.Key, conflict.TypeName, cultureName);
        }
    }

    // The root namespace an assembly's embedded config declares, which a host's folder and a pack lay its files out by.
    private string? GetRootNamespace(string assemblyName)
        => _embeddedByAssemblyName.TryGetValue(assemblyName, out var embeddedSource) ? embeddedSource.GetRootNamespace(assemblyName) : null;

    private readonly record struct CacheKey(ResourceLocation Location, string CultureName);
}

/// <summary>A translation together with the key it was read under.</summary>
internal readonly record struct JsonResourceEntry(string Name, string Format, bool IsSourceText);
