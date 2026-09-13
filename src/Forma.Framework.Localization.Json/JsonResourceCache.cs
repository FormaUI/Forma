namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

using Forma.Framework.Localization;

/// <summary>Reads the JSON translation files embedded in an assembly and keeps each one in memory after the first read.</summary>
internal sealed class JsonResourceCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly ConcurrentDictionary<CacheKey, FrozenDictionary<string, JsonResourceEntry>?> _files = new();

    /// <summary>The translations for one culture, or null when the assembly carries no file for it.</summary>
    public FrozenDictionary<string, JsonResourceEntry>? Read(Assembly assembly, string baseName, CultureInfo culture)
        => _files.GetOrAdd(new CacheKey(assembly, baseName, culture.Name), static key => Load(key));

    private static FrozenDictionary<string, JsonResourceEntry>? Load(CacheKey key)
    {
        var name = $"{key.BaseName}.{key.CultureName}.json";

        using var stream = key.Assembly.GetManifestResourceStream(name) ?? OpenSatellite(key);
        if (stream is null)
        {
            return null;
        }

        var entries = JsonSerializer.Deserialize<Dictionary<string, string?>>(stream, SerializerOptions) ?? [];

        LocalizationMetadata.Consume(entries, name, key.CultureName);

        // A null value says the translation is deliberately the key itself, so nothing downstream ever sees one.
        return entries.ToFrozenDictionary(
            entry => entry.Key,
            entry => new JsonResourceEntry(entry.Key, entry.Value ?? entry.Key, entry.Value is null),
            StringComparer.Ordinal);
    }

    // A file the build moved into the culture's satellite assembly, where its name carries no culture.
    private static Stream? OpenSatellite(CacheKey key)
    {
        try
        {
            var satellite = key.Assembly.GetSatelliteAssembly(CultureInfo.GetCultureInfo(key.CultureName));
            return satellite.GetManifestResourceStream($"{key.BaseName}.json");
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

    private readonly record struct CacheKey(Assembly Assembly, string BaseName, string CultureName);
}

/// <summary>A translation together with the key it was read under.</summary>
internal readonly record struct JsonResourceEntry(string Name, string Format, bool IsSourceText);
