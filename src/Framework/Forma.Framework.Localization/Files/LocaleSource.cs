namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text;

/// <summary>A store locale files are read from, each file placed at the anchor its assembly's paths read it for.</summary>
/// <param name="format">The format the files are written in.</param>
public abstract class LocaleSource(ILocaleFileFormat format)
{
    // Joins an anchor's segments into a key no segment can carry.
    private const char AnchorKeySeparator = '\u001F';

    private readonly ConcurrentDictionary<string, LocaleRoot> _roots = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<AnchorsKey, FrozenDictionary<string, string[]>> _anchors = new();
    private readonly ConcurrentDictionary<RootedSectionsKey, FrozenDictionary<string, string[]>> _rootedSections = new();
    private readonly ConcurrentDictionary<string, LocaleFile?> _files = new(StringComparer.Ordinal);
    private IReadOnlyList<string>? _names;

    /// <summary>The format the files are written in.</summary>
    protected ILocaleFileFormat Format { get; } = format ?? throw new ArgumentNullException(nameof(format));

    /// <summary>The character written between folders in this source's names: <see cref="Path.AltDirectorySeparatorChar"/> for a file path, <c>.</c> for an embedded resource's name.</summary>
    protected abstract char FolderSeparator { get; }

    /// <summary>The paths this source reads an assembly's files by when its <c>.locale.json</c> states none.</summary>
    protected virtual IReadOnlyList<LocalePath> DefaultPaths
        => LocalePath.Defaults;

    /// <summary>How this source compares names.</summary>
    protected virtual StringComparison NameComparison
        => StringComparison.Ordinal;

    /// <summary>The translations for a type in one culture, in the order they apply.</summary>
    /// <remarks>
    /// The entries are read anchor by anchor from the root down to the type, and within an anchor by the paths in their
    /// order and then the rooted sections naming the anchor, so a later entry overrides an earlier one.
    /// </remarks>
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
        var rootedSections = _rootedSections.GetOrAdd(new RootedSectionsKey(assemblyName, cultureName), CreateRootedSections);

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

            var key = anchorKey.ToString();
            foreach (var name in anchors.GetValueOrDefault(key, []))
            {
                if (_files.GetOrAdd(name, ReadFile, cultureName) is { } file)
                {
                    file.Collect(anchor, depth, entries, GetDisplayName(name));
                }
            }

            foreach (var name in rootedSections.GetValueOrDefault(key, []))
            {
                if (_files.GetOrAdd(name, ReadFile, cultureName) is { } file)
                {
                    file.CollectRooted(anchor, depth, entries, GetDisplayName(name));
                }
            }
        }

        return entries;
    }

    /// <summary>The full name of every type or namespace the assembly's paths place a file at in one culture.</summary>
    /// <param name="assemblyName">The name of the assembly the file belongs to.</param>
    /// <param name="name">The file's name in the source.</param>
    /// <param name="cultureName">The culture the file is read for.</param>
    /// <exception cref="LocaleConfigException">The assembly's config file is malformed, or the source holds more than one for it.</exception>
    public IReadOnlyList<string> GetAnchorNames(string assemblyName, string name, string cultureName)
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

                // An anchor outside the root namespace is a full name, read only by a type outside the root namespace.
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

    /// <summary>The root namespace an assembly's anchors are counted from: the one its config declares, or its name.</summary>
    /// <param name="assemblyName">The assembly's name.</param>
    /// <exception cref="LocaleConfigException">The assembly's config file is malformed, or the source holds more than one for it.</exception>
    public string GetRootNamespace(string assemblyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);

        return _roots.GetOrAdd(assemblyName, CreateRoot).RootNamespace;
    }

    /// <summary>Every name the source's own store holds, config files included, written with <see cref="FolderSeparator"/> between folders.</summary>
    protected abstract IEnumerable<string> EnumerateNames();

    /// <summary>The names a culture's own store holds apart from the source's, each written as the source's store would write it.</summary>
    /// <param name="cultureName">The culture.</param>
    protected virtual IEnumerable<string> EnumerateCultureNames(string cultureName)
        => [];

    /// <summary>Where an assembly's names begin by default.</summary>
    /// <param name="assemblyName">The assembly's name.</param>
    protected abstract string GetDefaultRootDir(string assemblyName);

    /// <summary>Where the names of a type outside the root namespace begin, given where the root's begin.</summary>
    /// <param name="rootDir">Where the root's names begin.</param>
    protected virtual string GetRootDirOutsideRootNamespace(string rootDir)
        => rootDir;

    /// <summary>The name a file is reported by.</summary>
    /// <param name="name">The file's name in the source.</param>
    protected virtual string GetDisplayName(string name)
        => name;

    /// <summary>Opens a file the source lists.</summary>
    /// <param name="name">The file's name in the source.</param>
    protected virtual Stream? OpenFile(string name)
        => null;

    /// <summary>The assembly's config with the folder it sits in.</summary>
    /// <param name="assemblyName">The assembly's name.</param>
    /// <exception cref="LocaleConfigException">The source holds more than one config file, or the file is malformed.</exception>
    protected virtual (string Folder, LocaleConfig Config)? GetConfig(string assemblyName)
    {
        var configFiles = new List<(string Folder, string Name)>();

        foreach (var name in GetNames())
        {
            if (TryGetConfigFolder(name, out var folder))
            {
                configFiles.Add((folder, name));
            }
        }

        if (configFiles is [var firstConfigFile, var secondConfigFile, ..])
        {
            throw new LocaleConfigException(
                $"'{GetDisplayName(firstConfigFile.Name)}' and '{GetDisplayName(secondConfigFile.Name)}' both configure '{assemblyName}'; an assembly has one '{LocaleConfig.FileName}', at its root.");
        }

        return configFiles is [var configFile] ? (configFile.Folder, ReadConfig(configFile.Name)) : null;
    }

    /// <summary>Reads a locale file the source lists.</summary>
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

    /// <summary>Reads a config file the source lists.</summary>
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

    private LocaleRoot CreateRoot(string assemblyName)
    {
        if (GetConfig(assemblyName) is not var (folder, config))
        {
            return new LocaleRoot(GetDefaultRootDir(assemblyName), assemblyName, LocaleConfig.DefaultLocalesDir, DefaultPaths);
        }

        var rootDir = config.RootDir is { } written ? JoinFolders(folder, written) : folder;

        return new LocaleRoot(rootDir, config.RootNamespace ?? assemblyName, config.LocalesDir ?? LocaleConfig.DefaultLocalesDir, config.Paths ?? DefaultPaths);
    }

    // A rootDir is written with '/' and sits beneath the config file's own folder.
    private string JoinFolders(string folder, string rootDir)
    {
        var writtenRoot = rootDir.Replace(Path.AltDirectorySeparatorChar, FolderSeparator);

        return folder.Length == 0 ? writtenRoot : $"{folder}{FolderSeparator}{writtenRoot}";
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

                // A path standing for the root alone speaks for the root namespace only.
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

    // A rooted section is found by reading every file the paths place in the culture once, and is placed at the anchor it names whatever file carries it.
    private FrozenDictionary<string, string[]> CreateRootedSections(RootedSectionsKey key)
    {
        var names = new List<(string AnchorKey, string Name)>();

        foreach (var isOutsideRootNamespace in (ReadOnlySpan<bool>)[false, true])
        {
            var anchors = _anchors.GetOrAdd(new AnchorsKey(key.AssemblyName, key.CultureName, isOutsideRootNamespace), CreateAnchors);
            foreach (var name in anchors.Values.SelectMany(placed => placed).Distinct(StringComparer.Ordinal))
            {
                LocaleFile? file;
                try
                {
                    file = _files.GetOrAdd(name, ReadFile, key.CultureName);
                }
                catch (LocaleFileException)
                {
                    // A file that cannot be read carries no rooted section; reading its own type reports what is wrong with it.
                    continue;
                }

                if (file is null)
                {
                    continue;
                }

                foreach (var (sectionName, _) in file.Root.Sections)
                {
                    if (LocaleFile.GetRootedSegments(sectionName) is { } segments)
                    {
                        names.Add((string.Join(AnchorKeySeparator, segments), name));
                    }
                }
            }
        }

        return names
            .Distinct()
            .GroupBy(placed => placed.AnchorKey, placed => placed.Name, StringComparer.Ordinal)
            .ToFrozenDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    }

    private readonly record struct AnchorsKey(string AssemblyName, string CultureName, bool IsOutsideRootNamespace);

    private readonly record struct RootedSectionsKey(string AssemblyName, string CultureName);

    private readonly record struct PlacedFile(string Name, int PathIndex, int Order);
}
