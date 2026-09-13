namespace Forma.Tests.Localization;

using Forma.Framework.Localization.Files;

/// <summary>A locale source over files held in memory, each named by its segments joined with slashes, read through a format that hands each back as it is.</summary>
public sealed class FakeLocaleSource : LocaleSource
{
    private readonly FakeLocaleFileFormat _format;

    public FakeLocaleSource()
        : this(new FakeLocaleFileFormat())
    {
    }

    private FakeLocaleSource(FakeLocaleFileFormat format)
        : base(format)
    {
        _format = format;
    }

    /// <summary>The files, each keyed by its name.</summary>
    public Dictionary<string, LocaleFile> Files => _format.Files;

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
    protected override Stream? OpenFile(IReadOnlyList<string> segments, string cultureName)
    {
        var name = GetFileName(segments, cultureName);
        Requests.Add(name);

        return Files.ContainsKey(name) ? new MemoryStream() : null;
    }
}
