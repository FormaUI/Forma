namespace Forma.Framework.Localization.Json;

/// <summary>The folder a host keeps its own translation files in, laid out by assembly and type.</summary>
/// <param name="path">The folder's full path, or empty when the host keeps no such folder.</param>
internal sealed class HostLocaleSource(string path) : LocaleSource
{
    private readonly string _path = path;

    /// <inheritdoc />
    protected override bool ReadsDeclaredRoots => true;

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetLeadingSegments(string assemblyName, bool rootedAtAssembly)
        => [assemblyName];

    /// <inheritdoc />
    protected override string GetName(IReadOnlyList<string> segments, string cultureName)
        => Path.Combine([_path, .. segments.Take(segments.Count - 1), $"{segments[^1]}.{cultureName}.json"]);

    /// <inheritdoc />
    protected override Stream? Open(IReadOnlyList<string> segments, string cultureName)
    {
        if (_path.Length == 0)
        {
            return null;
        }

        var name = GetName(segments, cultureName);

        return File.Exists(name) ? File.OpenRead(name) : null;
    }
}
