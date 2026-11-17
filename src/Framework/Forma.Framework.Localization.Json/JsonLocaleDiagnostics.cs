namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

using Forma.Framework.Localization.Files;

using Microsoft.Extensions.Logging;

/// <summary>The checks a factory's locales run on every lookup, and what they have reported.</summary>
/// <param name="cache">The cache whose entries a missing key is classified by.</param>
/// <param name="cultures">The cultures every lookup is checked in beside the culture it is made in.</param>
/// <param name="logger">The logger a missing key is reported through.</param>
internal sealed class JsonLocaleDiagnostics(JsonResourceCache cache, IReadOnlyList<CultureInfo> cultures, ILogger logger)
{
    private readonly ConcurrentDictionary<(Assembly Assembly, string CultureName), byte> _reportedAssemblies = new();
    private readonly ConcurrentDictionary<(ResourceLocation Location, string CultureName), byte> _reportedTypes = new();
    private readonly ConcurrentDictionary<(ResourceLocation Location, string Key, string CultureName), byte> _reportedKeys = new();
    private readonly ConcurrentDictionary<(ResourceLocation Location, string CultureName), byte> _reportedUnreadTypes = new();

    /// <summary>The cultures every lookup is checked in beside the culture it is made in.</summary>
    public IReadOnlyList<CultureInfo> Cultures
        => cultures;

    /// <summary>Reports a key that resolves to no entry in a culture, once, as the broadest part of its location the culture and its fallbacks hold nothing for.</summary>
    /// <remarks>
    /// An assembly without an entry in the culture or its fallbacks, in any source, is reported in place of every type and key
    /// beneath it, and a type without one in place of its keys, so each reaches the log once whichever lookup finds it first.
    /// </remarks>
    /// <param name="location">The location the key was looked up for.</param>
    /// <param name="name">The key as the lookup wrote it.</param>
    /// <param name="culture">The culture the key resolves to no entry in.</param>
    public void ReportMissing(ResourceLocation location, string name, CultureInfo culture)
    {
        if (!cache.HasAssemblyEntries(location, culture))
        {
            if (_reportedAssemblies.TryAdd((location.Assembly, culture.Name), 0))
            {
                JsonLocalizationLog.AssemblyNotLocalized(logger, location.AssemblyName, culture.Name, name, location.TypeName);
            }
        }
        else if (!cache.HasTypeEntries(location, culture))
        {
            if (_reportedTypes.TryAdd((location, culture.Name), 0))
            {
                JsonLocalizationLog.TypeNotLocalized(logger, location.TypeName, location.AssemblyName, culture.Name, name);
            }
        }
        else if (_reportedKeys.TryAdd((location, name, culture.Name), 0))
        {
            JsonLocalizationLog.KeyNotLocalized(logger, name, location.TypeName, location.AssemblyName, culture.Name);
        }
    }

    /// <summary>Reports, once, a location whose files in a culture checked beside the lookup's could not be read, which leaves its lookups unchecked there.</summary>
    /// <param name="location">The location whose files could not be read.</param>
    /// <param name="culture">The culture the files are read for.</param>
    /// <param name="exception">What reading them found wrong.</param>
    public void ReportUnread(ResourceLocation location, CultureInfo culture, LocaleFileException exception)
    {
        if (_reportedUnreadTypes.TryAdd((location, culture.Name), 0))
        {
            JsonLocalizationLog.LocaleFileNotRead(logger, location.TypeName, location.AssemblyName, culture.Name, exception.Message);
        }
    }
}
