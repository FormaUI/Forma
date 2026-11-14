namespace Forma.Framework.Localization.Files;

using System.Reflection;

/// <summary>The locale files an application embeds for other assemblies' types, known by their resource names beneath the application's root, each starting with the name of the assembly whose strings it holds.</summary>
/// <param name="application">The application the files are embedded in.</param>
/// <param name="format">The format the files are written in.</param>
/// <param name="rootNamespaces">The root namespace each assembly's own config declares, by the assembly's name.</param>
/// <remarks>
/// The application's names begin where its own files' do, at the folder its own config sits in or, where it has none, at
/// its name, so <c>.locales/es/Forma.Blazor/Components/Bars.json</c> in its project reads for <c>Forma.Blazor</c> as a
/// pack's file of that name does. An assembly's folder is laid out by the root namespace the assembly's own config
/// declares.
/// </remarks>
public sealed class ApplicationLocaleSource(Assembly application, ILocaleFileFormat format, Func<string, string?> rootNamespaces) : LocaleSource(format)
{
    private readonly Assembly _application = application ?? throw new ArgumentNullException(nameof(application));
    private readonly string _applicationName = application.GetName().Name ?? string.Empty;
    private readonly Func<string, string?> _rootNamespaces = rootNamespaces ?? throw new ArgumentNullException(nameof(rootNamespaces));
    private readonly EmbeddedLocaleSource _ownFiles = new(application, format);
    private string? _namesPrefix;

    /// <inheritdoc />
    protected override char FolderSeparator
        => '.';

    /// <inheritdoc />
    protected override bool NamesStartWithAssemblyName
        => true;

    // The application's own config is read once, at the first name asked for.
    private string NamesPrefix
        => _namesPrefix ??= _ownFiles.GetRootDir(_applicationName) + FolderSeparator;

    /// <inheritdoc />
    protected override IEnumerable<string> EnumerateNames()
    {
        var namesPrefix = NamesPrefix;

        return _application.GetManifestResourceNames()
            .Where(name => name.StartsWith(namesPrefix, StringComparison.Ordinal))
            .Select(name => name[namesPrefix.Length..]);
    }

    /// <inheritdoc />
    protected override string GetDefaultRootDir(string assemblyName)
        => assemblyName;

    // An assembly's config is embedded in the assembly, and the application's folder for it follows only the root namespace it declares.
    /// <inheritdoc />
    protected override (string Folder, LocaleConfig Config)? GetConfig(string assemblyName)
        => (assemblyName, new LocaleConfig(_rootNamespaces(assemblyName), null, null, null));

    /// <inheritdoc />
    protected override string GetDisplayName(string name)
        => $"{_applicationName}/{NamesPrefix}{name}";

    /// <inheritdoc />
    protected override Stream? OpenFile(string name)
        => _application.GetManifestResourceStream(NamesPrefix + name);
}
