namespace Forma.Tests.Localization;

using Forma.Framework.Localization.Files;

/// <summary>A locale source over files already read, each named by its folders joined with slashes and rooted at a folder named for the assembly.</summary>
public sealed class PreparedLocaleSource() : LocaleSource(new FakeLocaleFileFormat())
{
    /// <summary>The files, each keyed by its name.</summary>
    public Dictionary<string, LocaleFile> Files { get; } = new(StringComparer.Ordinal);

    /// <summary>The names asked for so far, in order.</summary>
    public List<string> Requests { get; } = [];

    /// <inheritdoc />
    protected override char FolderSeparator
        => Path.AltDirectorySeparatorChar;

    /// <inheritdoc />
    protected override IEnumerable<string> EnumerateNames()
        => Files.Keys;

    /// <inheritdoc />
    protected override string GetDefaultRootDir(string assemblyName)
        => assemblyName;

    /// <inheritdoc />
    protected override LocaleFile? ReadFile(string name, string cultureName)
    {
        Requests.Add(name);

        return Files.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    protected override Stream? OpenFile(string name)
        => throw new InvalidOperationException("A prepared source is never asked to open a file.");
}
