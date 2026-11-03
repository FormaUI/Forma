namespace Forma.Framework.Localization.Tests.Support;

using Forma.Framework.Localization.Files;

/// <summary>A locale source over files and configs held in memory, each named by its folders joined with slashes and rooted at a folder named for the assembly.</summary>
public sealed class FakeLocaleSource : LocaleSource
{
    private readonly FakeLocaleFileFormat _format;

    /// <summary>Creates an empty locale source.</summary>
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
