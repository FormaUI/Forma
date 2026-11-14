namespace Forma.Framework.Localization.Files;

using System.Runtime.ExceptionServices;

/// <summary>Every translation one source holds for an assembly's types in one culture, read from the source's files once.</summary>
public sealed class LocaleTranslations
{
    private readonly string[] _rootSegments;
    private readonly LocaleNames _underRoot;
    private readonly LocaleNames _outsideRoot;

    internal LocaleTranslations(string rootNamespace, LocaleNames underRoot, LocaleNames outsideRoot, IReadOnlyList<LocaleConflict> conflicts)
    {
        _rootSegments = rootNamespace.Split('.');
        _underRoot = underRoot;
        _outsideRoot = outsideRoot;
        Conflicts = conflicts;
    }

    /// <summary>The translations two files of the source give different texts.</summary>
    public IReadOnlyList<LocaleConflict> Conflicts { get; }

    /// <summary>Writes the translations the source holds for a type into <paramref name="entries"/>, keyed by their English source text, so each key is left with the text the source gives it.</summary>
    /// <remarks>
    /// A key's shared entries are written first, those of a shallower namespace before a deeper one's, and the type's own
    /// last, so a type's own entry wins over a shared one and a deeper shared entry over a shallower one. A file the source
    /// could not read throws for every type beneath the name it stands for.
    /// </remarks>
    /// <param name="typeName">The type's full name.</param>
    /// <param name="entries">The entries the translations are written into.</param>
    /// <exception cref="LocaleFileException">A file standing for a part of the type's name is malformed.</exception>
    public void Collect(string typeName, IDictionary<string, LocaleSourceEntry> entries)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(entries);

        var segments = typeName.Split('.');
        var isUnderRoot = LocaleSource.IsUnderRootNamespace(segments, _rootSegments);
        var anchor = isUnderRoot && segments.Length > 1 ? segments[_rootSegments.Length..] : segments;

        (isUnderRoot ? _underRoot : _outsideRoot).Collect(string.Join('.', anchor), entries);
    }
}

/// <summary>The translations a source holds for the names counted one way, from the root namespace or in full, keyed by those names.</summary>
/// <param name="rootNamespace">The namespace the names are counted from, empty for full names, which a conflict's type is named in full by.</param>
internal sealed class LocaleNames(string rootNamespace)
{
    private readonly Dictionary<string, Dictionary<string, LocaleSourceEntry>> _own = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, Dictionary<string, LocaleSourceEntry>> _shared = [with(StringComparer.Ordinal)];
    private readonly List<(string Name, ExceptionDispatchInfo Exception)> _broken = [];

    /// <summary>Records a file the source could not read, which every type beneath the name it stands for reports.</summary>
    /// <param name="anchor">The name the file stands for, its segments joined with dots.</param>
    /// <param name="exception">What reading the file found wrong.</param>
    public void AddBroken(string anchor, LocaleFileException exception)
        => _broken.Add((anchor, ExceptionDispatchInfo.Capture(exception)));

    /// <summary>Adds what a file holds, standing for a name, with its rooted sections added to every reading of names.</summary>
    /// <param name="anchor">The name the file stands for, its segments.</param>
    /// <param name="file">The file.</param>
    /// <param name="fileName">The name the file is reported by.</param>
    /// <param name="rooted">Every reading of names the source keeps, which a rooted section is added to.</param>
    /// <param name="conflicts">The conflicts found so far, which this reading's are added to, or null where another reading of the file records them.</param>
    public void Add(string[] anchor, LocaleFile file, string fileName, ReadOnlySpan<LocaleNames> rooted, List<LocaleConflict>? conflicts)
    {
        var path = new List<string>(anchor);

        foreach (var (name, section) in file.Root.Sections)
        {
            if (LocaleFile.GetRootedSegments(name) is { } segments)
            {
                foreach (var names in rooted)
                {
                    names.AddSection(section, [.. segments], fileName, name, names == this ? conflicts : null);
                }
            }
        }

        AddSection(file.Root, path, fileName, string.Empty, conflicts);
    }

    /// <summary>Writes the translations for a name into entries: its shared entries from the shallowest namespace down, then its own.</summary>
    /// <param name="name">The name, its segments joined with dots.</param>
    /// <param name="entries">The entries the translations are written into.</param>
    public void Collect(string name, IDictionary<string, LocaleSourceEntry> entries)
    {
        foreach (var (anchor, exception) in _broken)
        {
            if (IsPrefix(anchor, name))
            {
                exception.Throw();
            }
        }

        if (_shared.Count > 0)
        {
            var lookup = _shared.GetAlternateLookup<ReadOnlySpan<char>>();
            Write(lookup, string.Empty, entries);

            for (var end = name.IndexOf('.', StringComparison.Ordinal); end >= 0; end = name.IndexOf('.', end + 1, StringComparison.Ordinal))
            {
                Write(lookup, name.AsSpan(0, end), entries);
            }

            Write(lookup, name, entries);
        }

        if (_own.TryGetValue(name, out var own))
        {
            foreach (var (key, entry) in own)
            {
                entries[key] = entry;
            }
        }
    }

    private static void Write(Dictionary<string, Dictionary<string, LocaleSourceEntry>>.AlternateLookup<ReadOnlySpan<char>> lookup, ReadOnlySpan<char> name, IDictionary<string, LocaleSourceEntry> entries)
    {
        if (lookup.TryGetValue(name, out var shared))
        {
            foreach (var (key, entry) in shared)
            {
                entries[key] = entry;
            }
        }
    }

    // A name stands above another where it is empty, the same, or the other's start up to a dot.
    private static bool IsPrefix(string anchor, string name)
        => anchor.Length == 0
            || (name.StartsWith(anchor, StringComparison.Ordinal) && (name.Length == anchor.Length || name[anchor.Length] == '.'));

    private void AddEntry(Dictionary<string, Dictionary<string, LocaleSourceEntry>> translations, string name, LocaleSourceEntry entry, List<LocaleConflict>? conflicts)
    {
        if (!translations.TryGetValue(name, out var keys))
        {
            translations[name] = keys = [with(StringComparer.Ordinal)];
        }

        if (keys.TryGetValue(entry.Key, out var existing) && !string.Equals(existing.Value, entry.Value, StringComparison.Ordinal))
        {
            // The text a source keeps depends on no order its files are found in: the file whose name sorts last wins.
            var keepsNew = string.CompareOrdinal(entry.FileName, existing.FileName) >= 0;
            conflicts?.Add(new LocaleConflict(Join(rootNamespace, name), entry.Key, keepsNew ? entry.FileName : existing.FileName, keepsNew ? existing.FileName : entry.FileName));

            if (!keepsNew)
            {
                return;
            }
        }

        keys[entry.Key] = entry;
    }

    private static string Join(string first, string second)
        => first.Length == 0 ? second : $"{first}.{second}";

    private void AddSection(LocaleSection node, List<string> path, string fileName, string sectionName, List<LocaleConflict>? conflicts)
    {
        var name = string.Join('.', path);

        if (node.SharedEntries.Count > 0)
        {
            var sharedName = Join(sectionName, LocalizationSchema.SharedKey);
            foreach (var (key, value) in node.SharedEntries)
            {
                AddEntry(_shared, name, new LocaleSourceEntry(fileName, sharedName, key, value), conflicts);
            }
        }

        foreach (var (key, value) in node.Entries)
        {
            AddEntry(_own, name, new LocaleSourceEntry(fileName, sectionName, key, value), conflicts);
        }

        foreach (var (sectionKey, section) in node.Sections)
        {
            // A rooted section is placed where it names, from the file's top level alone.
            if (LocaleFile.GetRootedSegments(sectionKey) is not null)
            {
                continue;
            }

            var segments = sectionKey.Split('.');
            path.AddRange(segments);
            AddSection(section, path, fileName, Join(sectionName, sectionKey), conflicts);
            path.RemoveRange(path.Count - segments.Length, segments.Length);
        }
    }
}
