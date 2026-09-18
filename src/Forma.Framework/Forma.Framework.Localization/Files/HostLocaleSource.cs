namespace Forma.Framework.Localization.Files;

/// <summary>The folder a host keeps its own locale files in, a folder inside it for each assembly, known by their paths relative to it.</summary>
/// <param name="path">The folder's full path, or empty when the host keeps no such folder.</param>
/// <param name="format">The format the files are written in.</param>
public sealed class HostLocaleSource(string path, ILocaleFileFormat format) : LocaleSource(format)
{
    // A folder whose name starts with a dot is hidden to .NET on some systems, and a locale file sits in one by default.
    private static readonly EnumerationOptions WalkOptions = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.None,
        IgnoreInaccessible = true,
        MatchType = MatchType.Simple,
    };

    private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));

    /// <inheritdoc />
    protected override char FolderSeparator
        => Path.AltDirectorySeparatorChar;

    /// <inheritdoc />
    protected override IReadOnlyList<LocalePath> DefaultPaths
        => LocalePath.HostDefaults;

    // Names are compared as the file system the host most likely runs on compares them.
    /// <inheritdoc />
    protected override StringComparison NameComparison
        => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <inheritdoc />
    protected override IEnumerable<string> EnumerateNames()
        => _path.Length == 0 || !Directory.Exists(_path)
            ? []
            : Directory
                .EnumerateFiles(_path, "*", WalkOptions)
                .Select(file => Path.GetRelativePath(_path, file).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    /// <inheritdoc />
    protected override string GetDefaultRootDir(string assemblyName)
        => assemblyName;

    /// <inheritdoc />
    protected override bool BelongsToAssembly(string folder, string assemblyName)
        => folder.Equals(assemblyName, NameComparison) || LocaleRoot.IsBeneath(assemblyName, folder, FolderSeparator, NameComparison);

    /// <inheritdoc />
    protected override string GetDisplayName(string name)
        => Path.Combine(_path, name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

    /// <inheritdoc />
    protected override Stream? OpenFile(string name)
    {
        var fullPath = GetDisplayName(name);

        return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
    }
}
