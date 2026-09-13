namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

/// <summary>The translation files embedded in one assembly, in the assembly itself or in a culture's satellite assembly.</summary>
/// <param name="assembly">The assembly the files are embedded in.</param>
/// <param name="resourcesPath">The folder the files are embedded under, as dotted segments, or empty when they sit beside the types they translate.</param>
internal sealed class EmbeddedLocaleSource(Assembly assembly, string resourcesPath) : LocaleSource
{
    private readonly Assembly _assembly = assembly;
    private readonly string _resourcesPath = resourcesPath;
    private readonly ConcurrentDictionary<string, Assembly?> _satellites = new(StringComparer.Ordinal);

    /// <inheritdoc />
    protected override bool ReadsDeclaredRoots => false;

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetLeadingSegments(string assemblyName, bool rootedAtAssembly)
        => !rootedAtAssembly ? [] : _resourcesPath.Length == 0 ? [assemblyName] : [assemblyName, _resourcesPath];

    /// <inheritdoc />
    protected override string GetName(IReadOnlyList<string> segments, string cultureName)
        => $"{string.Join('.', segments)}.{cultureName}.json";

    /// <inheritdoc />
    protected override Stream? Open(IReadOnlyList<string> segments, string cultureName)
        => _assembly.GetManifestResourceStream(GetName(segments, cultureName))
            ?? _satellites.GetOrAdd(cultureName, GetSatellite)?.GetManifestResourceStream($"{string.Join('.', segments)}.json");

    // A file the build moved into the culture's satellite assembly is named there without its culture.
    private Assembly? GetSatellite(string cultureName)
    {
        try
        {
            return _assembly.GetSatelliteAssembly(CultureInfo.GetCultureInfo(cultureName));
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
}
