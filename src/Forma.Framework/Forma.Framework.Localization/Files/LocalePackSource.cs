namespace Forma.Framework.Localization.Files;

using System.Reflection;

/// <summary>The locale files a pack carries for other assemblies' types, a folder inside it for each assembly, known by their resource names; a pack is a host's folder that ships as an assembly, so an assembly's folder is laid out by the root namespace the assembly's own config declares and holds no config of its own.</summary>
/// <param name="pack">The assembly the files are embedded in.</param>
/// <param name="format">The format the files are written in.</param>
/// <param name="rootNamespaces">The root namespace each assembly's own config declares, by the assembly's name, or null where it declares none.</param>
public sealed class LocalePackSource(Assembly pack, ILocaleFileFormat format, Func<string, string?> rootNamespaces) : LocaleSource(format)
{
    private readonly Assembly _pack = pack ?? throw new ArgumentNullException(nameof(pack));
    private readonly string _packName = pack.GetName().Name ?? string.Empty;
    private readonly Func<string, string?> _rootNamespaces = rootNamespaces ?? throw new ArgumentNullException(nameof(rootNamespaces));

    /// <inheritdoc />
    protected override char FolderSeparator
        => '.';

    // A pack is read as a host's folder is, so a file sits in a folder's own locales folder or directly in the folder.
    /// <inheritdoc />
    protected override IReadOnlyList<LocalePath> DefaultPaths
        => LocalePath.HostDefaults;

    /// <inheritdoc />
    protected override IEnumerable<string> EnumerateNames()
        => _pack.GetManifestResourceNames();

    /// <inheritdoc />
    protected override string GetDefaultRootDir(string assemblyName)
        => assemblyName;

    // A config file is the assembly's own, embedded at its root, so a pack carries none; the one fact a pack's layout follows is the root namespace the assembly declares.
    /// <inheritdoc />
    protected override (string Folder, LocaleConfig Config)? GetConfig(string assemblyName)
        => (assemblyName, new LocaleConfig(_rootNamespaces(assemblyName), null, null, null));

    // Several packs can carry a file for one anchor, so what is reported says which package to open.
    /// <inheritdoc />
    protected override string GetDisplayName(string name)
        => $"{_packName}/{name}";

    /// <inheritdoc />
    protected override Stream? OpenFile(string name)
        => _pack.GetManifestResourceStream(name);
}
