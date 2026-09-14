namespace Forma.Framework.Localization.Files;

/// <summary>The folder a host keeps its own translation files in, laid out by assembly and type.</summary>
/// <param name="path">The folder's full path, or empty when the host keeps no such folder.</param>
/// <param name="format">The format the files are written in.</param>
public sealed class HostLocaleSource(string path, ILocaleFileFormat format) : LocaleSource(format)
{
    private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));

    /// <inheritdoc />
    protected override bool ReadsDeclaredRoots => true;

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetLeadingSegments(string assemblyName, bool rootedAtAssembly)
        => [assemblyName];

    /// <inheritdoc />
    protected override string GetFileName(IReadOnlyList<string> segments, string cultureName)
        => Path.Combine([_path, .. segments.Take(segments.Count - 1), $"{segments[^1]}.{cultureName}{Format.Extension}"]);

    /// <inheritdoc />
    protected override Stream? OpenFile(IReadOnlyList<string> segments, string cultureName)
    {
        if (_path.Length == 0)
        {
            return null;
        }

        var name = GetFileName(segments, cultureName);

        return File.Exists(name) ? File.OpenRead(name) : null;
    }
}
