namespace Forma.Tests.Localization;

using Forma.Framework.Localization.Files;

/// <summary>A locale source over files already read, each named by its segments joined with slashes, which opens nothing.</summary>
public sealed class PreparedLocaleSource() : LocaleSource(new FakeLocaleFileFormat())
{
    /// <summary>The files, each keyed by its name.</summary>
    public Dictionary<string, LocaleFile> Files { get; } = new(StringComparer.Ordinal);

    /// <summary>The names asked for so far, in order.</summary>
    public List<string> Requests { get; } = [];

    /// <inheritdoc />
    protected override bool ReadsDeclaredRoots => true;

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetLeadingSegments(string assemblyName, bool rootedAtAssembly)
        => [assemblyName];

    /// <inheritdoc />
    protected override string GetFileName(IReadOnlyList<string> segments, string cultureName)
        => $"{string.Join('/', segments)}.{cultureName}{Format.Extension}";

    /// <inheritdoc />
    protected override LocaleFile? ReadFile(IReadOnlyList<string> segments, string cultureName)
    {
        var name = GetFileName(segments, cultureName);
        Requests.Add(name);

        return Files.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    protected override Stream? OpenFile(IReadOnlyList<string> segments, string cultureName)
        => throw new InvalidOperationException("A prepared source is never asked to open a file.");
}
