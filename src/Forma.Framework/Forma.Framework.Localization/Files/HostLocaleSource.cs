namespace Forma.Framework.Localization.Files;

/// <summary>The folder a host keeps its own locale files in, with a folder inside it for each assembly, the files known by their paths relative to it.</summary>
/// <param name="path">The folder's full path.</param>
/// <param name="format">The format the files are written in.</param>
/// <param name="rootNamespaces">The root namespace each assembly's own config declares, by the assembly's name.</param>
/// <remarks>An assembly's folder is laid out by the root namespace the assembly's own config declares.</remarks>
public sealed class HostLocaleSource(string path, ILocaleFileFormat format, Func<string, string?> rootNamespaces) : LocaleSource(format)
{
    // A folder whose name starts with a dot is hidden on some systems, and the default folder of locale files is one.
    private static readonly EnumerationOptions WalkOptions = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.None,
        IgnoreInaccessible = true,
        MatchType = MatchType.Simple,
    };

    private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));
    private readonly Func<string, string?> _rootNamespaces = rootNamespaces ?? throw new ArgumentNullException(nameof(rootNamespaces));

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

    // An assembly's config is embedded in the assembly, and the host's folder follows only the root namespace it declares.
    /// <inheritdoc />
    protected override (string Folder, LocaleConfig Config)? GetConfig(string assemblyName)
        => (assemblyName, new LocaleConfig(_rootNamespaces(assemblyName), null, null, null));

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
