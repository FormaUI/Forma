namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

using Forma.Framework.Localization;

/// <summary>Reads the translations for a type in one culture, the file embedded in its assembly with the host's own entries laid over it, and keeps each result in memory after the first read.</summary>
/// <param name="locales">The folder the host keeps its own translation files in.</param>
internal sealed class JsonResourceCache(LocalesFolder locales)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly LocalesFolder _locales = locales;
    private readonly ConcurrentDictionary<CacheKey, FrozenDictionary<string, JsonResourceEntry>?> _files = new();

    /// <summary>The translations for one culture, or null when neither the assembly nor the host carries any for it.</summary>
    public FrozenDictionary<string, JsonResourceEntry>? Read(ResourceLocation location, CultureInfo culture)
        => _files.GetOrAdd(new CacheKey(location, culture.Name), Load);

    private FrozenDictionary<string, JsonResourceEntry>? Load(CacheKey key)
    {
        var (assembly, baseName, typeName) = key.Location;

        var entries = ReadEmbedded(assembly, baseName, key.CultureName);
        var hostEntries = _locales.Read(assembly.GetName().Name ?? string.Empty, typeName, key.CultureName);
        if (entries is null && hostEntries.Count == 0)
        {
            return null;
        }

        entries ??= new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (name, translation) in hostEntries)
        {
            entries[name] = translation;
        }

        // A null value says the translation is deliberately the key itself, so nothing downstream ever sees one.
        return entries.ToFrozenDictionary(
            entry => entry.Key,
            entry => new JsonResourceEntry(entry.Key, entry.Value ?? entry.Key, entry.Value is null),
            StringComparer.Ordinal);
    }

    private static Dictionary<string, string?>? ReadEmbedded(Assembly assembly, string baseName, string cultureName)
    {
        var name = $"{baseName}.{cultureName}.json";

        using var stream = assembly.GetManifestResourceStream(name) ?? OpenSatellite(assembly, baseName, cultureName);
        if (stream is null)
        {
            return null;
        }

        var entries = JsonSerializer.Deserialize<Dictionary<string, string?>>(stream, SerializerOptions) ?? [];

        LocalizationMetadata.Consume(entries, name, cultureName);

        return entries;
    }

    // A file the build moved into the culture's satellite assembly, where its name carries no culture.
    private static Stream? OpenSatellite(Assembly assembly, string baseName, string cultureName)
    {
        try
        {
            var satellite = assembly.GetSatelliteAssembly(CultureInfo.GetCultureInfo(cultureName));
            return satellite.GetManifestResourceStream($"{baseName}.json");
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private readonly record struct CacheKey(ResourceLocation Location, string CultureName);
}

/// <summary>A translation together with the key it was read under.</summary>
internal readonly record struct JsonResourceEntry(string Name, string Format, bool IsSourceText);
