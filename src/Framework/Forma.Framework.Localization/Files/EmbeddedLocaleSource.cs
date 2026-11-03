namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

/// <summary>The locale files embedded in one assembly, in the assembly itself or in a culture's satellite assembly, known by their resource names.</summary>
/// <param name="assembly">The assembly the files are embedded in.</param>
/// <param name="format">The format the files are written in.</param>
public sealed class EmbeddedLocaleSource(Assembly assembly, ILocaleFileFormat format) : LocaleSource(format)
{
    private readonly Assembly _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    private readonly ConcurrentDictionary<string, SatelliteResource> _satelliteResources = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string[]> _satelliteNames = new(StringComparer.Ordinal);

    /// <inheritdoc />
    protected override char FolderSeparator
        => '.';

    /// <inheritdoc />
    protected override IEnumerable<string> EnumerateNames()
        => _assembly.GetManifestResourceNames();

    // A file the build moved into the culture's satellite assembly is named there without its culture, which sat just before the extension.
    /// <inheritdoc />
    protected override IEnumerable<string> EnumerateCultureNames(string cultureName)
        => _satelliteNames.GetOrAdd(cultureName, ListSatelliteNames);

    /// <inheritdoc />
    protected override string GetDefaultRootDir(string assemblyName)
        => assemblyName;

    // The build puts a namespace at the front of every resource name, so a type outside the root namespace is spelled from the start.
    /// <inheritdoc />
    protected override string GetRootDirOutsideRootNamespace(string rootDir)
        => string.Empty;

    /// <inheritdoc />
    protected override Stream? OpenFile(string name)
        => _assembly.GetManifestResourceStream(name)
            ?? (_satelliteResources.TryGetValue(name, out var resource) ? resource.Satellite.GetManifestResourceStream(resource.Name) : null);

    private string[] ListSatelliteNames(string cultureName)
    {
        if (GetSatellite(cultureName) is not { } satellite)
        {
            return [];
        }

        var names = new List<string>();
        foreach (var resourceName in satellite.GetManifestResourceNames())
        {
            if (!resourceName.EndsWith(Format.Extension, StringComparison.Ordinal))
            {
                continue;
            }

            var name = $"{resourceName[..^Format.Extension.Length]}.{cultureName}{Format.Extension}";
            _satelliteResources[name] = new SatelliteResource(satellite, resourceName);
            names.Add(name);
        }

        return [.. names];
    }

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

    private readonly record struct SatelliteResource(Assembly Satellite, string Name);
}
