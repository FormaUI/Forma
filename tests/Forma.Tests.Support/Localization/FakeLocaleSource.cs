namespace Forma.Tests.Localization;

using Forma.Framework.Localization.Files;

/// <summary>A locale source over files held in memory, each named by its folders joined with slashes and rooted at a folder named for the assembly, read through a format that hands each back as it is.</summary>
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

    /// <summary>The locale files, each keyed by its name.</summary>
    public Dictionary<string, LocaleFile> Files => _format.Files;

    /// <summary>The config files, each keyed by its name.</summary>
    public Dictionary<string, LocaleConfig> Configs { get; } = new(StringComparer.Ordinal);

    /// <summary>The names opened so far, in order.</summary>
    public List<string> Requests { get; } = [];

    /// <summary>The full name of every type or namespace the paths in force place a file at in one culture.</summary>
    /// <param name="assemblyName">The name of the assembly the file belongs to.</param>
    /// <param name="name">The file's name.</param>
    /// <param name="cultureName">The culture the file is read for.</param>
    public new IReadOnlyList<string> GetAnchorNames(string assemblyName, string name, string cultureName)
        => base.GetAnchorNames(assemblyName, name, cultureName);

    /// <inheritdoc />
    protected override char FolderSeparator
        => Path.AltDirectorySeparatorChar;

    /// <inheritdoc />
    protected override IEnumerable<string> EnumerateNames()
        => Files.Keys.Concat(Configs.Keys);

    /// <inheritdoc />
    protected override string GetDefaultRootDir(string assemblyName)
        => assemblyName;

    /// <inheritdoc />
    protected override Stream? OpenFile(string name)
    {
        Requests.Add(name);

        return Files.ContainsKey(name) ? new MemoryStream() : null;
    }

    /// <inheritdoc />
    protected override LocaleConfig ReadConfig(string name)
        => Configs[name];
}
