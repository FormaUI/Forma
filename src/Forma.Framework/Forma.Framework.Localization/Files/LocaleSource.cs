namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text;

/// <summary>Where locale files are read from: every name the source holds, listed once, each placed at the anchor it stands for by its assembly's paths.</summary>
/// <param name="format">The format the files are written in.</param>
public abstract class LocaleSource(ILocaleFileFormat format)
{
    // Joins an anchor's segments into a key no segment can carry.
    private const char AnchorKeySeparator = '\u001F';

    private readonly ConcurrentDictionary<string, LocaleRoot> _roots = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<AnchorsKey, FrozenDictionary<string, string[]>> _anchors = new();
    private readonly ConcurrentDictionary<string, LocaleFile?> _files = new(StringComparer.Ordinal);
    private IReadOnlyList<string>? _names;

    /// <summary>The format the files are written in.</summary>
    protected ILocaleFileFormat Format { get; } = format ?? throw new ArgumentNullException(nameof(format));

    /// <summary>The character written between folders in this source's names: <see cref="Path.AltDirectorySeparatorChar"/> for a file path, <c>.</c> for an embedded resource's name, which no path separator describes.</summary>
    protected abstract char FolderSeparator { get; }

    /// <summary>The paths this source reads an assembly's files by where it has no <c>.locale.json</c> or its file states none, <see cref="LocalePath.Defaults"/> unless the source reads more.</summary>
    protected virtual IReadOnlyList<LocalePath> DefaultPaths
        => LocalePath.Defaults;

    /// <summary>How this source compares names, <see cref="StringComparison.Ordinal"/> unless its store ignores case.</summary>
    protected virtual StringComparison NameComparison
        => StringComparison.Ordinal;

    /// <summary>The translations for a type in one culture: anchor by anchor from the root down to the type, and within an anchor by the paths in their order, so a later entry overrides an earlier one.</summary>
    /// <param name="assemblyName">The name of the assembly the type lives in.</param>
    /// <param name="typeName">The type's full name.</param>
    /// <param name="cultureName">The culture the files are named for.</param>
    /// <exception cref="LocaleConfigException">The assembly's config file is malformed, or the source holds more than one for it.</exception>
    public IReadOnlyList<LocaleSourceEntry> Read(string assemblyName, string typeName, string cultureName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentException.ThrowIfNullOrEmpty(cultureName);

        var root = _roots.GetOrAdd(assemblyName, CreateRoot);
        var segments = typeName.Split('.');
        var rootSegments = root.RootNamespace.Split('.');

        var isUnderRoot = IsUnderRootNamespace(segments, rootSegments);
        var anchor = isUnderRoot && segments.Length > 1 ? segments[rootSegments.Length..] : segments;
        var anchors = _anchors.GetOrAdd(new AnchorsKey(assemblyName, cultureName, !isUnderRoot), CreateAnchors);

        var entries = new List<LocaleSourceEntry>();
        var anchorKey = new StringBuilder();

        for (var depth = 0; depth <= anchor.Length; depth++)
        {
            if (depth > 1)
            {
                anchorKey.Append(AnchorKeySeparator);
            }

            if (depth > 0)
            {
                anchorKey.Append(anchor[depth - 1]);
            }

            if (!anchors.TryGetValue(anchorKey.ToString(), out var names))
            {
                continue;
            }

            foreach (var name in names)
            {
                if (_files.GetOrAdd(name, ReadFile, cultureName) is { } file)
                {
                    file.Collect(anchor.AsSpan(depth), entries, GetDisplayName(name));
                }
            }
        }

        return entries;
    }

    /// <summary>The full name of every type or namespace the assembly's paths place a file at in one culture, none when no path reads it there.</summary>
    /// <param name="assemblyName">The name of the assembly the file belongs to.</param>
    /// <param name="name">The file's name in the source.</param>
    /// <param name="cultureName">The culture the file is read for.</param>
    /// <exception cref="LocaleConfigException">The assembly's config file is malformed, or the source holds more than one for it.</exception>
    protected IReadOnlyList<string> GetAnchorNames(string assemblyName, string name, string cultureName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(cultureName);

        var root = _roots.GetOrAdd(assemblyName, CreateRoot);
        var rootSegments = root.RootNamespace.Split('.');
        var anchorNames = new List<string>();

        foreach (var isOutsideRootNamespace in (ReadOnlySpan<bool>)[false, true])
        {
            var anchors = _anchors.GetOrAdd(new AnchorsKey(assemblyName, cultureName, isOutsideRootNamespace), CreateAnchors);
            foreach (var (anchorKey, names) in anchors)
            {
                if (!names.Any(candidate => candidate.Equals(name, NameComparison)))
                {
                    continue;
                }

                string[] anchor = anchorKey.Length == 0 ? [] : anchorKey.Split(AnchorKeySeparator);

                // An anchor outside the root namespace is a full name, which only a type the root namespace does not start is read by.
                if (isOutsideRootNamespace)
                {
                    if (anchor.Length > 0 && !IsUnderRootNamespace(anchor, rootSegments))
                    {
                        anchorNames.Add(string.Join('.', anchor));
                    }

                    continue;
                }

                anchorNames.Add(string.Join('.', [root.RootNamespace, .. anchor]));
            }
        }

        return [.. anchorNames.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>Every name the source's own store holds, config files included, written with <see cref="FolderSeparator"/> between folders.</summary>
    protected abstract IEnumerable<string> EnumerateNames();

    /// <summary>The names a culture's own store holds apart from the source's, each written as the source's store would write it; none unless the source keeps such stores.</summary>
    /// <param name="cultureName">The culture.</param>
    protected virtual IEnumerable<string> EnumerateCultureNames(string cultureName)
        => [];

    /// <summary>Where an assembly's names begin when it has no config file or its file names no <c>rootDir</c>.</summary>
    /// <param name="assemblyName">The assembly's name.</param>
    protected abstract string GetDefaultRootDir(string assemblyName);

    /// <summary>Where the names of a type outside the root namespace begin, given where the root's begin; the same place unless the store spells a namespace at the front of every name.</summary>
    /// <param name="rootDir">Where the root's names begin.</param>
    protected virtual string GetRootDirOutsideRootNamespace(string rootDir)
        => rootDir;

    /// <summary>Where names begin when a root's <c>rootDir</c> climbs to or past where the source's names start; refused unless the source says where such a root lands.</summary>
    /// <param name="configName">The config file naming the <c>rootDir</c>.</param>
    /// <param name="rootDir">The <c>rootDir</c> as written.</param>
    /// <param name="rootNamespace">The root namespace the config file puts in force.</param>
    /// <exception cref="LocaleConfigException">The source refuses such a root.</exception>
    protected virtual string GetRootDirPastStart(string configName, string rootDir, string rootNamespace)
        => throw new LocaleConfigException(
            $"'{GetDisplayName(configName)}' names '{rootDir}' as '{LocaleConfig.RootDirKey}', which climbs to or past where the source's names begin.");

    /// <summary>Whether a config file in a folder belongs to an assembly's files; every file does in a source holding one assembly's.</summary>
    /// <param name="folder">The folder the file sits in.</param>
    /// <param name="assemblyName">The assembly's name.</param>
    protected virtual bool BelongsToAssembly(string folder, string assemblyName)
        => true;

    /// <summary>The name a file has in anything thrown or reported, the name itself unless the source knows it better.</summary>
    /// <param name="name">The file's name in the source.</param>
    protected virtual string GetDisplayName(string name)
        => name;

    /// <summary>Opens a file the source lists, or null when it cannot; a source that reads its files itself opens none.</summary>
    /// <param name="name">The file's name in the source.</param>
    protected virtual Stream? OpenFile(string name)
        => null;

    /// <summary>Reads a locale file the source lists, or null when it cannot be opened; opens it for the format to read unless a derived class already holds it read.</summary>
    /// <param name="name">The file's name in the source.</param>
    /// <param name="cultureName">The culture the file's name gives it.</param>
    protected virtual LocaleFile? ReadFile(string name, string cultureName)
    {
        if (OpenFile(name) is not { } stream)
        {
            return null;
        }

        using (stream)
        {
            return Format.Read(stream, GetDisplayName(name), cultureName);
        }
    }

    /// <summary>Reads a config file the source lists, or a config that says nothing when it cannot be opened.</summary>
    /// <param name="name">The file's name in the source.</param>
    /// <exception cref="LocaleConfigException">The file is malformed.</exception>
    protected virtual LocaleConfig ReadConfig(string name)
    {
        if (OpenFile(name) is not { } stream)
        {
            return LocaleConfig.Empty;
        }

        using (stream)
        {
            return LocaleConfig.Read(stream, GetDisplayName(name));
        }
    }

    // A type outside any namespace belongs to the root, and a type starting elsewhere is read by its full name.
    private static bool IsUnderRootNamespace(string[] segments, string[] rootSegments)
        => segments.Length == 1
            || (segments.Length > rootSegments.Length && segments.AsSpan(0, rootSegments.Length).SequenceEqual(rootSegments));

    // A project or solution file stands for the folder it sits in.
    private static bool IsProjectOrSolutionFile(string name)
    {
        var extension = Path.GetExtension(name);

        return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            || (extension.Length >= ".proj".Length && extension.EndsWith("proj", StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyList<string> GetNames()
        => LazyInitializer.EnsureInitialized(ref _names, () => [.. EnumerateNames()]);

    private bool TryGetConfigFolder(string name, out string folder)
    {
        folder = string.Empty;
        if (name.Equals(LocaleConfig.FileName, NameComparison))
        {
            return true;
        }

        var folderLength = name.Length - LocaleConfig.FileName.Length - 1;
        if (folderLength > 0 && name[folderLength] == FolderSeparator && name.EndsWith(LocaleConfig.FileName, NameComparison))
        {
            folder = name[..folderLength];

            return true;
        }

        return false;
    }

    // An assembly has at most one config file, which marks where its names begin and puts its keys in force for every file beneath; without one its files read by the defaults.
    private LocaleRoot CreateRoot(string assemblyName)
    {
        var configFiles = new List<(string Folder, string Name)>();

        foreach (var name in GetNames())
        {
            if (TryGetConfigFolder(name, out var folder) && BelongsToAssembly(folder, assemblyName))
            {
                configFiles.Add((folder, name));
            }
        }

        if (configFiles is [var first, var second, ..])
        {
            throw new LocaleConfigException(
                $"'{GetDisplayName(first.Name)}' and '{GetDisplayName(second.Name)}' both configure '{assemblyName}'; an assembly has one '{LocaleConfig.FileName}'.");
        }

        if (configFiles is not [var configFile])
        {
            return new LocaleRoot(GetDefaultRootDir(assemblyName), assemblyName, LocaleConfig.DefaultLocalesDir, DefaultPaths);
        }

        var config = ReadConfig(configFile.Name);
        var rootNamespace = config.RootNamespace ?? assemblyName;
        var rootDir = config.RootDir is { } written ? ResolveRootDir(configFile, written, rootNamespace) : configFile.Folder;

        return new LocaleRoot(rootDir, rootNamespace, config.LocalesDir ?? LocaleConfig.DefaultLocalesDir, config.Paths ?? DefaultPaths);
    }

    // A rootDir is walked from the config file's own folder, and one ending in a project or solution file names that file's folder.
    private string ResolveRootDir((string Folder, string Name) configFile, string rootDir, string rootNamespace)
    {
        List<string> segments = configFile.Folder.Length == 0 ? [] : [.. configFile.Folder.Split(FolderSeparator)];
        var parts = rootDir.Split(Path.AltDirectorySeparatorChar);
        var folderCount = IsProjectOrSolutionFile(parts[^1]) ? parts.Length - 1 : parts.Length;

        foreach (var part in parts.AsSpan(0, folderCount))
        {
            switch (part)
            {
                case ".":
                    break;

                case "..":
                    if (segments.Count <= 1)
                    {
                        return GetRootDirPastStart(configFile.Name, rootDir, rootNamespace);
                    }

                    segments.RemoveAt(segments.Count - 1);
                    break;

                default:
                    segments.Add(part);
                    break;
            }
        }

        return string.Join(FolderSeparator, segments);
    }

    // Each file is placed at every anchor the assembly's paths read it for, in the order of the paths.
    private FrozenDictionary<string, string[]> CreateAnchors(AnchorsKey key)
    {
        var root = _roots.GetOrAdd(key.AssemblyName, CreateRoot);
        var rootDir = key.IsOutsideRootNamespace ? GetRootDirOutsideRootNamespace(root.RootDir) : root.RootDir;
        var rootNamespace = key.IsOutsideRootNamespace ? string.Empty : root.RootNamespace;

        var nameComparer = StringComparer.FromComparison(NameComparison);
        var values = new LocalePathValues(key.AssemblyName, rootNamespace, rootDir, root.LocalesDir, key.CultureName, Format.Extension);
        var matcher = new LocalePathMatcher(values, FolderSeparator, NameComparison);
        var placedFiles = new Dictionary<string, List<PlacedFile>>(nameComparer);
        var order = 0;

        foreach (var name in GetNames().Concat(EnumerateCultureNames(key.CultureName)))
        {
            if (TryGetConfigFolder(name, out _))
            {
                continue;
            }

            for (var pathIndex = 0; pathIndex < root.Paths.Count; pathIndex++)
            {
                var path = root.Paths[pathIndex];

                // A path standing for the root alone speaks for the root namespace, which a type outside it does not start with.
                if ((key.IsOutsideRootNamespace && path.IsRootOnly) || !matcher.TryGetAnchor(path, name, out var anchor))
                {
                    continue;
                }

                var anchorKey = string.Join(AnchorKeySeparator, anchor);
                if (!placedFiles.TryGetValue(anchorKey, out var files))
                {
                    placedFiles[anchorKey] = files = [];
                }

                files.Add(new PlacedFile(name, pathIndex, order++));
            }
        }

        return placedFiles.ToFrozenDictionary(
            pair => pair.Key,
            pair => pair.Value
                .OrderBy(file => file.PathIndex)
                .ThenBy(file => file.Order)
                .Select(file => file.Name)
                .Distinct(nameComparer)
                .ToArray(),
            nameComparer);
    }

    private readonly record struct AnchorsKey(string AssemblyName, string CultureName, bool IsOutsideRootNamespace);

    private readonly record struct PlacedFile(string Name, int PathIndex, int Order);
}
