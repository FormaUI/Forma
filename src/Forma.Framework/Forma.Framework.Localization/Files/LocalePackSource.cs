namespace Forma.Framework.Localization.Files;

using System.Reflection;

/// <summary>The locale files a pack carries for other assemblies' types, with a folder inside it for each assembly, the files known by their resource names.</summary>
/// <param name="pack">The assembly the files are embedded in.</param>
/// <param name="format">The format the files are written in.</param>
/// <param name="rootNamespaces">The root namespace each assembly's own config declares, by the assembly's name.</param>
/// <remarks>A pack is read as a host's folder is: an assembly's folder is laid out by the root namespace the assembly's own config declares.</remarks>
public sealed class LocalePackSource(Assembly pack, ILocaleFileFormat format, Func<string, string?> rootNamespaces) : LocaleSource(format)
{
    private readonly Assembly _pack = pack ?? throw new ArgumentNullException(nameof(pack));
    private readonly string _packName = pack.GetName().Name ?? string.Empty;
    private readonly Func<string, string?> _rootNamespaces = rootNamespaces ?? throw new ArgumentNullException(nameof(rootNamespaces));

    /// <inheritdoc />
    protected override char FolderSeparator
        => '.';

    /// <inheritdoc />
    protected override IReadOnlyList<LocalePath> DefaultPaths
        => LocalePath.HostDefaults;

    /// <inheritdoc />
    protected override IEnumerable<string> EnumerateNames()
        => _pack.GetManifestResourceNames();

    /// <inheritdoc />
    protected override string GetDefaultRootDir(string assemblyName)
        => assemblyName;

    // An assembly's config is embedded in the assembly, and a pack's folder follows only the root namespace it declares.
    /// <inheritdoc />
    protected override (string Folder, LocaleConfig Config)? GetConfig(string assemblyName)
        => (assemblyName, new LocaleConfig(_rootNamespaces(assemblyName), null, null, null));

    /// <inheritdoc />
    protected override string GetDisplayName(string name)
        => $"{_packName}/{name}";

    /// <inheritdoc />
    protected override Stream? OpenFile(string name)
        => _pack.GetManifestResourceStream(name);
}
