namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Collections.Frozen;

/// <summary>A store locale files are read from, each file placed at the anchor its assembly's paths read it for.</summary>
/// <param name="format">The format the files are written in.</param>
public abstract class LocaleSource(ILocaleFileFormat format)
{
    // Joins an anchor's segments into a key no segment can carry.
    private const char AnchorKeySeparator = '\u001F';

    private readonly ConcurrentDictionary<string, LocaleRoot> _roots = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<AnchorsKey, FrozenDictionary<string, string[]>> _anchors = new();
    private readonly ConcurrentDictionary<EntriesKey, LocaleEntries> _entries = new();
    private readonly ConcurrentDictionary<string, LocaleFile?> _files = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<(string Name, string LocalesDir), LocaleFileName?> _fileNames = new();
    private IReadOnlyList<string>? _names;

    /// <summary>The format the files are written in.</summary>
    protected ILocaleFileFormat Format { get; } = format ?? throw new ArgumentNullException(nameof(format));

    /// <summary>Whether every name the source holds starts with the name of the assembly whose strings it holds, as a pack's and a host's do.</summary>
    protected virtual bool NamesStartWithAssemblyName
        => false;

    /// <summary>How this source compares names.</summary>
    protected virtual StringComparison NameComparison
        => StringComparison.Ordinal;

    /// <summary>The entries the source holds for a type in one culture, one for each key, as the source settles them.</summary>
    /// <remarks>A key's shared entries give way to deeper ones and to the type's own, and two files giving one entry different texts are a conflict, which <see cref="ReadEntries"/> lists and the file whose name sorts last settles.</remarks>
    /// <param name="assemblyName">The name of the assembly the type lives in.</param>
    /// <param name="typeName">The type's full name.</param>
    /// <param name="cultureName">The culture the files are named for.</param>
    /// <exception cref="LocaleConfigException">The assembly's config file is malformed, or the source holds more than one for it.</exception>
    /// <exception cref="LocaleFileException">A file standing for a part of the type's name is malformed.</exception>
    public IReadOnlyList<LocaleSourceEntry> Read(string assemblyName, string typeName, string cultureName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        var entries = new Dictionary<string, LocaleSourceEntry>(StringComparer.Ordinal);
        ReadEntries(assemblyName, cultureName).Collect(typeName, entries);

        return [.. entries.Values];
    }

    /// <summary>Every entry the source holds for an assembly's types in one culture, read from every file the assembly's paths place in the culture, once.</summary>
    /// <param name="assemblyName">The name of the assembly the types live in.</param>
    /// <param name="cultureName">The culture the files are named for.</param>
    /// <exception cref="LocaleConfigException">The assembly's config file is malformed, or the source holds more than one for it.</exception>
    public LocaleEntries ReadEntries(string assemblyName, string cultureName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);
        ArgumentException.ThrowIfNullOrEmpty(cultureName);

        return _entries.GetOrAdd(new EntriesKey(assemblyName, cultureName), CreateEntries);
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

    /// <summary>Where an assembly's names begin: the folder its config sits in, and the folder the config names beneath it, or where the source's names begin by default.</summary>
    /// <param name="assemblyName">The assembly's name.</param>
    /// <exception cref="LocaleConfigException">The assembly's config file is malformed, or the source holds more than one for it.</exception>
    public string GetRootDir(string assemblyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);

        return _roots.GetOrAdd(assemblyName, CreateRoot).RootDir;
    }

    /// <summary>The folder of locale files an assembly's names leave out: the one its config names, or <c>.locales</c>.</summary>
    /// <param name="assemblyName">The assembly's name.</param>
    /// <exception cref="LocaleConfigException">The assembly's config file is malformed, or the source holds more than one for it.</exception>
    public string GetLocalesDir(string assemblyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);

        return _roots.GetOrAdd(assemblyName, CreateRoot).LocalesDir;
    }

    /// <summary>Every name the source's own store holds, config files included, written with <c>/</c> between folders.</summary>
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

    /// <summary>Whether the source holds a config file for an assembly.</summary>
    /// <param name="assemblyName">The assembly's name.</param>
    internal bool HasConfig(string assemblyName)
        => GetConfig(assemblyName) is not null;

    // A type outside any namespace belongs to the root, and a type starting elsewhere is read by its full name.
    internal static bool IsUnderRootNamespace(string[] segments, string[] rootSegments)
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
        if (folderLength > 0 && name[folderLength] == Path.AltDirectorySeparatorChar && name.EndsWith(LocaleConfig.FileName, NameComparison))
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
            return new LocaleRoot(GetDefaultRootDir(assemblyName), assemblyName, LocaleConfig.DefaultLocalesDir, null);
        }

        var rootDir = config.RootDir is { } written ? JoinFolders(folder, written) : folder;

        return new LocaleRoot(rootDir, config.RootNamespace ?? assemblyName, config.LocalesDir ?? LocaleConfig.DefaultLocalesDir, config.Paths);
    }

    // A rootDir sits beneath the config file's own folder.
    private static string JoinFolders(string folder, string rootDir)
        => folder.Length == 0 ? rootDir : $"{folder}{Path.AltDirectorySeparatorChar}{rootDir}";

    // Each file is placed at the anchor the default reading takes from its name, or at every anchor the assembly's stated
    // paths read it for, in the order of the paths.
    private FrozenDictionary<string, string[]> CreateAnchors(AnchorsKey key)
    {
        var root = _roots.GetOrAdd(key.AssemblyName, CreateRoot);
        var rootDir = key.IsOutsideRootNamespace ? GetRootDirOutsideRootNamespace(root.RootDir) : root.RootDir;

        if (root.Paths is not { } paths)
        {
            return PlaceByName(key, root, rootDir);
        }

        var rootNamespace = key.IsOutsideRootNamespace ? string.Empty : root.RootNamespace;

        var nameComparer = StringComparer.FromComparison(NameComparison);
        var values = new LocalePathValues(key.AssemblyName, rootNamespace, rootDir, root.LocalesDir, key.CultureName, Format.Extension);
        var matcher = new LocalePathMatcher(values, NameComparison);
        var placedFiles = new Dictionary<string, List<PlacedFile>>(nameComparer);
        var order = 0;

        foreach (var name in GetNames().Concat(EnumerateCultureNames(key.CultureName)))
        {
            if (TryGetConfigFolder(name, out _))
            {
                continue;
            }

            for (var pathIndex = 0; pathIndex < paths.Count; pathIndex++)
            {
                var path = paths[pathIndex];

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

    // The default reading: a name is read from where the assembly's names begin or, in a source whose names start with an
    // assembly's name, from the source's root with that name leading the parts its culture and folders of locale files
    // leave, so the culture can stand before the assembly's name or after it.
    private FrozenDictionary<string, string[]> PlaceByName(AnchorsKey key, LocaleRoot root, string rootDir)
    {
        var nameComparer = StringComparer.FromComparison(NameComparison);
        var assemblyParts = key.AssemblyName.Split('.');
        var rootParts = NamesStartWithAssemblyName || rootDir.Length == 0 ? [] : LocaleFileNames.Split(rootDir);
        var rootNamespaceParts = root.RootNamespace.Split('.');
        var placedFiles = new Dictionary<string, List<string>>(nameComparer);

        foreach (var name in GetNames().Concat(EnumerateCultureNames(key.CultureName)))
        {
            // A name is read once whatever culture or reading asks for it, and only where it can be for the culture asked for.
            if (TryGetConfigFolder(name, out _)
                || !LocaleFileNames.HasPart(name, key.CultureName)
                || _fileNames.GetOrAdd((name, root.LocalesDir), ReadFileName) is not { } fileName
                || !string.Equals(fileName.Culture, key.CultureName, StringComparison.Ordinal)
                || !LocaleFileNames.StartsWith(fileName.Parts.AsSpan(), rootParts, NameComparison))
            {
                continue;
            }

            var start = rootParts.Length;

            if (NamesStartWithAssemblyName)
            {
                if (!LocaleFileNames.StartsWith(fileName.Parts.AsSpan()[start..], assemblyParts, NameComparison))
                {
                    continue;
                }

                start += assemblyParts.Length;
            }

            // Read in full, a name starting with the root namespace stands for types the reading under the root namespace
            // answers, so it reaches no type here, and the root's own file speaks for the root namespace only.
            if (key.IsOutsideRootNamespace
                && (start == fileName.Parts.Length || LocaleFileNames.StartsWith(fileName.Parts.AsSpan()[start..], rootNamespaceParts, StringComparison.Ordinal)))
            {
                continue;
            }

            var anchorKey = string.Join(AnchorKeySeparator, fileName.Parts.AsSpan()[start..]);
            if (!placedFiles.TryGetValue(anchorKey, out var files))
            {
                placedFiles[anchorKey] = files = [];
            }

            files.Add(name);
        }

        return placedFiles.ToFrozenDictionary(pair => pair.Key, pair => pair.Value.Distinct(nameComparer).ToArray(), nameComparer);
    }

    private LocaleFileName? ReadFileName((string Name, string LocalesDir) key)
        => LocaleFileNames.TryRead(key.Name, key.LocalesDir, Format.Extension, NameComparison, out var fileName) ? fileName : null;

    // Every file the paths place in the culture is read once, its entries placed by the name it stands for and its
    // rooted sections by the names they spell, which every reading of names takes, as a type may be counted either way.
    private LocaleEntries CreateEntries(EntriesKey key)
    {
        var root = _roots.GetOrAdd(key.AssemblyName, CreateRoot);
        var underRoot = new LocaleNames(root.RootNamespace);
        var outsideRoot = new LocaleNames(string.Empty);
        var placedUnderRoot = new HashSet<string>(StringComparer.Ordinal);

        foreach (var isOutsideRootNamespace in (ReadOnlySpan<bool>)[false, true])
        {
            var names = isOutsideRootNamespace ? outsideRoot : underRoot;
            var anchors = _anchors.GetOrAdd(new AnchorsKey(key.AssemblyName, key.CultureName, isOutsideRootNamespace), CreateAnchors);

            foreach (var (anchorKey, fileNames) in anchors)
            {
                string[] anchor = anchorKey.Length == 0 ? [] : anchorKey.Split(AnchorKeySeparator);

                foreach (var name in fileNames)
                {
                    LocaleFile? file;
                    try
                    {
                        file = _files.GetOrAdd(name, ReadFile, key.CultureName);
                    }
                    catch (LocaleFileException exception)
                    {
                        // Outside a folder of locale files, the default reading takes only what reads as a locale file;
                        // anywhere else, a file that cannot be read is reported by every type beneath the name it stands for.
                        if (root.Paths is not null || _fileNames.GetOrAdd((name, root.LocalesDir), ReadFileName) is { IsInLocalesDir: true })
                        {
                            names.AddBroken(string.Join('.', anchor), exception);
                        }

                        continue;
                    }

                    if (file is null)
                    {
                        continue;
                    }

                    // A file read under the root namespace records its conflicts there, since reading it in full as well
                    // names types no assembly has; a file read in full alone records them in full.
                    if (!isOutsideRootNamespace)
                    {
                        placedUnderRoot.Add(name);
                    }

                    var recordsConflicts = !isOutsideRootNamespace || !placedUnderRoot.Contains(name);
                    names.Add(anchor, file, GetDisplayName(name), [underRoot, outsideRoot], recordsConflicts);
                }
            }
        }

        return new LocaleEntries(root.RootNamespace, underRoot, outsideRoot);
    }

    private readonly record struct AnchorsKey(string AssemblyName, string CultureName, bool IsOutsideRootNamespace);

    private readonly record struct EntriesKey(string AssemblyName, string CultureName);

    private readonly record struct PlacedFile(string Name, int PathIndex, int Order);
}
