namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text;

/// <summary>Where locale files are read from: every name the source holds, listed once, each placed at the anchor it stands for by the paths in force where it sits.</summary>
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

    /// <summary>The paths this source reads by where no <c>.locali.json</c> states others, <see cref="LocalePath.Defaults"/> unless the source reads more.</summary>
    protected virtual IReadOnlyList<LocalePath> DefaultPaths
        => LocalePath.Defaults;

    /// <summary>How this source compares names, <see cref="StringComparison.Ordinal"/> unless its store ignores case.</summary>
    protected virtual StringComparison NameComparison
        => StringComparison.Ordinal;

    /// <summary>The translations for a type in one culture: anchor by anchor from the root down to the type, and within an anchor by the paths in their order, so a later entry overrides an earlier one.</summary>
    /// <param name="assemblyName">The name of the assembly the type lives in.</param>
    /// <param name="typeName">The type's full name.</param>
    /// <param name="cultureName">The culture the files are named for.</param>
    /// <exception cref="LocaliSettingsException">A settings file the source holds for the assembly is malformed, or the source's settings mark more than one root.</exception>
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

    /// <summary>The full name of every type or namespace the paths in force place a file at in one culture, none when no path reads it there.</summary>
    /// <param name="assemblyName">The name of the assembly the file belongs to.</param>
    /// <param name="name">The file's name in the source.</param>
    /// <param name="cultureName">The culture the file is read for.</param>
    /// <exception cref="LocaliSettingsException">A settings file the source holds for the assembly is malformed, or the source's settings mark more than one root.</exception>
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

    /// <summary>Every name the source's own store holds, settings files included, written with <see cref="FolderSeparator"/> between folders.</summary>
    protected abstract IEnumerable<string> EnumerateNames();

    /// <summary>The names a culture's own store holds apart from the source's, each written as the source's store would write it; none unless the source keeps such stores.</summary>
    /// <param name="cultureName">The culture.</param>
    protected virtual IEnumerable<string> EnumerateCultureNames(string cultureName)
        => [];

    /// <summary>Where an assembly's names begin when no settings file says otherwise.</summary>
    /// <param name="assemblyName">The assembly's name.</param>
    protected abstract string GetDefaultRootDir(string assemblyName);

    /// <summary>Where the names of a type outside the root namespace begin, given where the root's begin; the same place unless the store spells a namespace at the front of every name.</summary>
    /// <param name="rootDir">Where the root's names begin.</param>
    protected virtual string GetRootDirOutsideRootNamespace(string rootDir)
        => rootDir;

    /// <summary>Where names begin when a root's <c>rootDir</c> climbs to or past where the source's names start; refused unless the source says where such a root lands.</summary>
    /// <param name="settingsName">The settings file naming the <c>rootDir</c>.</param>
    /// <param name="rootDir">The <c>rootDir</c> as written.</param>
    /// <param name="rootNamespace">The root namespace the settings file puts in force.</param>
    /// <exception cref="LocaliSettingsException">The source refuses such a root.</exception>
    protected virtual string GetRootDirPastStart(string settingsName, string rootDir, string rootNamespace)
        => throw new LocaliSettingsException(
            $"'{GetDisplayName(settingsName)}' names '{rootDir}' as '{LocaliSettings.RootDirKey}', which climbs to or past where the source's names begin.");

    /// <summary>Whether a settings file in a folder applies to every assembly's files, beneath whatever root each has, rather than to one assembly's.</summary>
    /// <param name="folder">The folder the file sits in.</param>
    protected virtual bool AppliesToEveryAssembly(string folder)
        => false;

    /// <summary>Whether a settings file in a folder belongs to an assembly's files; every file does in a source holding one assembly's.</summary>
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

    /// <summary>Reads a settings file the source lists, or settings that say nothing when it cannot be opened.</summary>
    /// <param name="name">The file's name in the source.</param>
    /// <exception cref="LocaliSettingsException">The file is malformed.</exception>
    protected virtual LocaliSettings ReadSettings(string name)
    {
        if (OpenFile(name) is not { } stream)
        {
            return LocaliSettings.Empty;
        }

        using (stream)
        {
            return LocaliSettings.Read(stream, GetDisplayName(name));
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

    private bool TryGetSettingsFolder(string name, out string folder)
    {
        folder = string.Empty;
        if (name.Equals(LocaliSettings.FileName, NameComparison))
        {
            return true;
        }

        var folderLength = name.Length - LocaliSettings.FileName.Length - 1;
        if (folderLength > 0 && name[folderLength] == FolderSeparator && name.EndsWith(LocaliSettings.FileName, NameComparison))
        {
            folder = name[..folderLength];

            return true;
        }

        return false;
    }

    // The outermost settings file an assembly's files hold marks their root; a nested one applies its paths and folder name beneath it and moves no root.
    private LocaleRoot CreateRoot(string assemblyName)
    {
        var sharedFiles = new List<(string Folder, string Name)>();
        var ownFiles = new List<(string Folder, string Name)>();

        foreach (var name in GetNames())
        {
            if (!TryGetSettingsFolder(name, out var folder))
            {
                continue;
            }

            if (AppliesToEveryAssembly(folder))
            {
                sharedFiles.Add((folder, name));
            }
            else if (BelongsToAssembly(folder, assemblyName))
            {
                ownFiles.Add((folder, name));
            }
        }

        var outermostFiles = ownFiles
            .Where(file => !ownFiles.Any(other => other.Folder.Length < file.Folder.Length && LocaleRoot.IsBeneath(other.Folder, file.Folder, FolderSeparator, NameComparison)))
            .ToList();

        if (outermostFiles is [var first, var second, ..])
        {
            throw new LocaliSettingsException(
                $"'{GetDisplayName(first.Name)}' and '{GetDisplayName(second.Name)}' each mark a root for '{assemblyName}'; one settings file must sit above both.");
        }

        var rootSettings = LocaleFolderSettings.CreateDefault(DefaultPaths);
        foreach (var (folder, name) in sharedFiles)
        {
            rootSettings = rootSettings.Apply(folder, ReadSettings(name));
        }

        var rootNamespace = assemblyName;
        var rootDir = GetDefaultRootDir(assemblyName);

        if (outermostFiles is [var rootFile])
        {
            var settings = ReadSettings(rootFile.Name);
            rootNamespace = settings.RootNamespace ?? assemblyName;
            rootDir = settings.RootDir is { } written ? ResolveRootDir(rootFile, written, rootNamespace) : rootFile.Folder;
            rootSettings = rootSettings.Apply(rootFile.Folder, settings);
        }

        var nestedSettings = new List<LocaleFolderSettings>();
        foreach (var (folder, name) in ownFiles.Where(file => !outermostFiles.Contains(file)).OrderBy(file => file.Folder.Length))
        {
            var parentSettings = nestedSettings.Where(candidate => LocaleRoot.IsBeneath(candidate.Folder, folder, FolderSeparator, NameComparison)).MaxBy(candidate => candidate.Depth)
                ?? rootSettings;

            nestedSettings.Add(parentSettings.Apply(folder, ReadSettings(name)));
        }

        return new LocaleRoot(rootDir, rootNamespace, rootSettings, nestedSettings, FolderSeparator, NameComparison);
    }

    // A rootDir is walked from the settings file's own folder, and one ending in a project or solution file names that file's folder.
    private string ResolveRootDir((string Folder, string Name) rootFile, string rootDir, string rootNamespace)
    {
        List<string> segments = rootFile.Folder.Length == 0 ? [] : [.. rootFile.Folder.Split(FolderSeparator)];
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
                        return GetRootDirPastStart(rootFile.Name, rootDir, rootNamespace);
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

    // Each file is placed at every anchor a path in force where it sits reads it for; within an anchor a farther folder's files come before a nearer one's, and each folder's in the order of its paths.
    private FrozenDictionary<string, string[]> CreateAnchors(AnchorsKey key)
    {
        var root = _roots.GetOrAdd(key.AssemblyName, CreateRoot);
        var rootDir = key.IsOutsideRootNamespace ? GetRootDirOutsideRootNamespace(root.RootDir) : root.RootDir;
        var rootNamespace = key.IsOutsideRootNamespace ? string.Empty : root.RootNamespace;

        var nameComparer = StringComparer.FromComparison(NameComparison);
        var matchers = new Dictionary<LocaleFolderSettings, LocalePathMatcher>();
        var placedFiles = new Dictionary<string, List<PlacedFile>>(nameComparer);
        var order = 0;

        foreach (var name in GetNames().Concat(EnumerateCultureNames(key.CultureName)))
        {
            if (TryGetSettingsFolder(name, out _))
            {
                continue;
            }

            var settings = root.GetSettings(name);
            if (!matchers.TryGetValue(settings, out var matcher))
            {
                var values = new LocalePathValues(key.AssemblyName, rootNamespace, rootDir, settings.LocalesDir, key.CultureName, Format.Extension);
                matchers[settings] = matcher = new LocalePathMatcher(values, FolderSeparator, NameComparison);
            }

            for (var pathIndex = 0; pathIndex < settings.Paths.Count; pathIndex++)
            {
                var path = settings.Paths[pathIndex];

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

                files.Add(new PlacedFile(name, settings.Depth, pathIndex, order++));
            }
        }

        return placedFiles.ToFrozenDictionary(
            pair => pair.Key,
            pair => pair.Value
                .OrderBy(file => file.Depth)
                .ThenBy(file => file.PathIndex)
                .ThenBy(file => file.Order)
                .Select(file => file.Name)
                .Distinct(nameComparer)
                .ToArray(),
            nameComparer);
    }

    private readonly record struct AnchorsKey(string AssemblyName, string CultureName, bool IsOutsideRootNamespace);

    private readonly record struct PlacedFile(string Name, int Depth, int PathIndex, int Order);
}
