namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

/// <summary>The locale files embedded in one assembly, in the assembly itself or in a culture's satellite assembly.</summary>
/// <param name="assembly">The assembly the files are embedded in.</param>
/// <param name="resourcesPath">The folder the files are embedded under, as dotted segments, or empty when they sit beside the types they localize.</param>
/// <param name="format">The format the files are written in.</param>
public sealed class EmbeddedLocaleSource(Assembly assembly, string resourcesPath, ILocaleFileFormat format) : LocaleSource(format)
{
    private readonly Assembly _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    private readonly string _resourcesPath = resourcesPath ?? throw new ArgumentNullException(nameof(resourcesPath));
    private readonly ConcurrentDictionary<string, Assembly?> _satellites = new(StringComparer.Ordinal);

    /// <inheritdoc />
    protected override bool ReadsDeclaredRoots => false;

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetLeadingSegments(string assemblyName, bool rootedAtAssembly)
        => !rootedAtAssembly ? [] : _resourcesPath.Length == 0 ? [assemblyName] : [assemblyName, _resourcesPath];

    /// <inheritdoc />
    protected override string GetFileName(IReadOnlyList<string> segments, string cultureName)
        => $"{string.Join('.', segments)}.{cultureName}{Format.Extension}";

    /// <inheritdoc />
    protected override Stream? OpenFile(IReadOnlyList<string> segments, string cultureName)
        => _assembly.GetManifestResourceStream(GetFileName(segments, cultureName))
            ?? _satellites.GetOrAdd(cultureName, GetSatellite)?.GetManifestResourceStream($"{string.Join('.', segments)}{Format.Extension}");

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
