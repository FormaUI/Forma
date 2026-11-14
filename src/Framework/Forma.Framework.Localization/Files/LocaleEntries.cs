namespace Forma.Framework.Localization.Files;

using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

/// <summary>Every entry one source holds for an assembly's types in one culture, read from the source's files once.</summary>
public sealed class LocaleEntries
{
    private readonly string _rootNamespace;
    private readonly LocaleNames _underRoot;
    private readonly LocaleNames _outsideRoot;
    private readonly Lazy<IReadOnlyList<LocaleConflict>> _conflicts;

    internal LocaleEntries(string rootNamespace, LocaleNames underRoot, LocaleNames outsideRoot)
    {
        _rootNamespace = rootNamespace;
        _underRoot = underRoot;
        _outsideRoot = outsideRoot;
        _conflicts = new(() => [.. underRoot.GetConflicts().Concat(outsideRoot.GetConflicts()).Distinct()]);
    }

    /// <summary>The entries two files of the source give different texts.</summary>
    public IReadOnlyList<LocaleConflict> Conflicts
        => _conflicts.Value;

    /// <summary>Writes the entries the source holds for a type into <paramref name="entries"/>, keyed by their source text, so each key is left with the text the source gives it.</summary>
    /// <remarks>
    /// A key's shared entries are written first, those of a shallower namespace before a deeper one's, and the type's own
    /// last, so a type's own entry wins over a shared one and a deeper shared entry over a shallower one. Of two files
    /// giving one of them different texts, the file whose name sorts last wins. A file the source could not read throws for
    /// every type beneath the name it stands for.
    /// </remarks>
    /// <param name="typeName">The type's full name.</param>
    /// <param name="entries">The entries written to, keyed by their source text.</param>
    /// <param name="conflicts">The conflicts the type's entries meet, added to as they are written.</param>
    /// <exception cref="LocaleFileException">A file standing for a part of the type's name is malformed.</exception>
    public void Collect(string typeName, IDictionary<string, LocaleSourceEntry> entries, ICollection<LocaleConflict>? conflicts = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(entries);

        var names = GetNames(typeName, out var nameStart);
        names.Collect(typeName.AsSpan(nameStart), entries, conflicts);
    }

    /// <summary>Every section of the source's files a type reads, whether or not its entries win, in the order they are written.</summary>
    /// <param name="typeName">The type's full name.</param>
    /// <exception cref="LocaleFileException">A file standing for a part of the type's name is malformed.</exception>
    public IReadOnlyList<LocaleSourceSection> GetSections(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        var names = GetNames(typeName, out var nameStart);
        var sections = new List<LocaleSourceSection>();
        names.CollectSections(typeName.AsSpan(nameStart), sections);

        return [.. sections.Distinct()];
    }

    // The names a type is read by, and where its name starts among them: from the root namespace for a type beneath it,
    // and in full for one outside it. A type outside any namespace is read under the root namespace by its own name.
    private LocaleNames GetNames(string typeName, out int nameStart)
    {
        nameStart = 0;

        if (!typeName.Contains('.', StringComparison.Ordinal))
        {
            return _underRoot;
        }

        if (typeName.Length > _rootNamespace.Length && typeName[_rootNamespace.Length] == '.' && typeName.StartsWith(_rootNamespace, StringComparison.Ordinal))
        {
            nameStart = _rootNamespace.Length + 1;

            return _underRoot;
        }

        return _outsideRoot;
    }
}

/// <summary>The sections a source's files hold for the names counted one way, from the root namespace or in full, keyed by those names.</summary>
/// <remarks>A section's entries are copied out only when a type it holds is read, so reading a culture's files costs what parsing them costs.</remarks>
/// <param name="rootNamespace">The namespace the names are counted from, empty for full names, which a conflict's type is named in full by.</param>
internal sealed class LocaleNames(string rootNamespace)
{
    private readonly Dictionary<string, LocaleNodes> _own = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, LocaleNodes> _shared = [with(StringComparer.Ordinal)];
    private readonly List<(string Name, ExceptionDispatchInfo Exception)> _broken = [];

    /// <summary>Records a file the source could not read, which every type beneath the name it stands for reports.</summary>
    /// <param name="anchor">The name the file stands for, its segments joined with dots.</param>
    /// <param name="exception">What reading the file found wrong.</param>
    public void AddBroken(string anchor, LocaleFileException exception)
        => _broken.Add((anchor, ExceptionDispatchInfo.Capture(exception)));

    /// <summary>Adds the sections a file holds, standing for a name, with its rooted sections added to every reading of names.</summary>
    /// <param name="anchor">The name the file stands for, its segments.</param>
    /// <param name="file">The file.</param>
    /// <param name="fileName">The name the file is reported by.</param>
    /// <param name="rooted">Every reading of names the source keeps, which a rooted section is added to.</param>
    /// <param name="recordsConflicts">Whether this reading reports the file's conflicts, which another reading of the file reports otherwise.</param>
    public void Add(string[] anchor, LocaleFile file, string fileName, ReadOnlySpan<LocaleNames> rooted, bool recordsConflicts)
    {
        foreach (var (name, section) in file.Root.Sections)
        {
            if (LocaleFile.GetRootedSegments(name) is { } segments)
            {
                foreach (var names in rooted)
                {
                    names.AddSection(section, [.. segments], fileName, name, names == this && recordsConflicts);
                }
            }
        }

        AddSection(file.Root, [.. anchor], fileName, string.Empty, recordsConflicts);
    }

    /// <summary>Writes a name's entries into <paramref name="entries"/>: its shared entries from the shallowest namespace down, then its own.</summary>
    /// <param name="name">The name, its segments joined with dots.</param>
    /// <param name="entries">The entries written to, keyed by their source text.</param>
    /// <param name="conflicts">The conflicts the entries meet, added to as they are written.</param>
    public void Collect(ReadOnlySpan<char> name, IDictionary<string, LocaleSourceEntry> entries, ICollection<LocaleConflict>? conflicts)
    {
        ThrowIfBroken(name);

        if (_shared.Count > 0)
        {
            var lookup = _shared.GetAlternateLookup<ReadOnlySpan<char>>();
            Write(lookup, string.Empty, isShared: true, entries, conflicts);

            for (var end = name.IndexOf('.'); end >= 0; end = NextDot(name, end))
            {
                Write(lookup, name[..end], isShared: true, entries, conflicts);
            }

            Write(lookup, name, isShared: true, entries, conflicts);
        }

        if (_own.Count > 0)
        {
            Write(_own.GetAlternateLookup<ReadOnlySpan<char>>(), name, isShared: false, entries, conflicts);
        }
    }

    /// <summary>Adds the sections a name reads to <paramref name="sections"/>, in the order <see cref="Collect"/> writes their entries: its shared sections from the shallowest namespace down, then its own.</summary>
    /// <param name="name">The name, its segments joined with dots.</param>
    /// <param name="sections">The sections the name's are added to.</param>
    public void CollectSections(ReadOnlySpan<char> name, ICollection<LocaleSourceSection> sections)
    {
        ThrowIfBroken(name);

        if (_shared.Count > 0)
        {
            var lookup = _shared.GetAlternateLookup<ReadOnlySpan<char>>();
            AddSections(lookup, string.Empty, isShared: true, sections);

            for (var end = name.IndexOf('.'); end >= 0; end = NextDot(name, end))
            {
                AddSections(lookup, name[..end], isShared: true, sections);
            }

            AddSections(lookup, name, isShared: true, sections);
        }

        if (_own.Count > 0)
        {
            AddSections(_own.GetAlternateLookup<ReadOnlySpan<char>>(), name, isShared: false, sections);
        }
    }

    /// <summary>Every conflict among the sections the reading reports conflicts for.</summary>
    public IEnumerable<LocaleConflict> GetConflicts()
    {
        var conflicts = new List<LocaleConflict>();

        foreach (var (name, nodes) in _shared)
        {
            if (nodes.Rest is not null)
            {
                Settle(name, nodes.ToList(), isShared: true, conflicts);
            }
        }

        foreach (var (name, nodes) in _own)
        {
            if (nodes.Rest is not null)
            {
                Settle(name, nodes.ToList(), isShared: false, conflicts);
            }
        }

        return conflicts;
    }

    // A name stands above another where it is empty, the same, or the other's start up to a dot.
    private static bool IsPrefix(string anchor, ReadOnlySpan<char> name)
        => anchor.Length == 0
            || (name.StartsWith(anchor, StringComparison.Ordinal) && (name.Length == anchor.Length || name[anchor.Length] == '.'));

    // The position of the dot after the one at dot, or -1 where none follows.
    private static int NextDot(ReadOnlySpan<char> name, int dot)
    {
        var next = name[(dot + 1)..].IndexOf('.');

        return next < 0 ? -1 : dot + 1 + next;
    }

    // An empty part adds nothing, so the root's own name is its namespace alone.
    private static string Join(string first, string second)
        => first.Length == 0 ? second : second.Length == 0 ? first : $"{first}.{second}";

    private static IReadOnlyList<KeyValuePair<string, string?>> GetEntries(LocaleNode node, bool isShared)
        => isShared ? node.Section.SharedEntries : node.Section.Entries;

    private static LocaleSourceEntry ToEntry(LocaleNode node, string key, string? value, bool isShared)
        => new(node.FileName, GetTypeName(node, isShared), key, value);

    // A shared entry is reported under its section's name and the shared section's reserved name.
    private static string GetTypeName(LocaleNode node, bool isShared)
        => isShared ? Join(node.SectionName, LocalizationSchema.SharedKey) : node.SectionName;

    // Sections giving one name are listed in the order their entries are applied, by their files' names.
    private static void AddSections(Dictionary<string, LocaleNodes>.AlternateLookup<ReadOnlySpan<char>> lookup, ReadOnlySpan<char> name, bool isShared, ICollection<LocaleSourceSection> sections)
    {
        if (!lookup.TryGetValue(name, out var nodes))
        {
            return;
        }

        IEnumerable<LocaleNode> ordered = nodes.Rest is null ? [nodes.First] : nodes.ToList().OrderBy(node => node.FileName, StringComparer.Ordinal);
        foreach (var node in ordered)
        {
            sections.Add(new LocaleSourceSection(node.FileName, GetTypeName(node, isShared)));
        }
    }

    // A file the source could not read throws for every name beneath the one it stands for.
    private void ThrowIfBroken(ReadOnlySpan<char> name)
    {
        foreach (var (anchor, exception) in _broken)
        {
            if (IsPrefix(anchor, name))
            {
                exception.Throw();
            }
        }
    }

    private void Write(Dictionary<string, LocaleNodes>.AlternateLookup<ReadOnlySpan<char>> lookup, ReadOnlySpan<char> name, bool isShared, IDictionary<string, LocaleSourceEntry> entries, ICollection<LocaleConflict>? conflicts)
    {
        if (!lookup.TryGetValue(name, out var nodes))
        {
            return;
        }

        if (nodes.Rest is null)
        {
            foreach (var (key, value) in GetEntries(nodes.First, isShared))
            {
                entries[key] = ToEntry(nodes.First, key, value, isShared);
            }

            return;
        }

        foreach (var (key, entry) in Settle(name.ToString(), nodes.ToList(), isShared, conflicts))
        {
            entries[key] = entry;
        }
    }

    // Sections giving one name's keys are applied in the order of their files' names, so the file whose name sorts last
    // wins whatever order the files were found in, and a key two of them give different texts is a conflict.
    private Dictionary<string, LocaleSourceEntry> Settle(string name, List<LocaleNode> nodes, bool isShared, ICollection<LocaleConflict>? conflicts)
    {
        var settled = new Dictionary<string, LocaleSourceEntry>(StringComparer.Ordinal);

        foreach (var node in nodes.OrderBy(node => node.FileName, StringComparer.Ordinal))
        {
            foreach (var (key, value) in GetEntries(node, isShared))
            {
                if (settled.TryGetValue(key, out var existing) && !string.Equals(existing.Value, value, StringComparison.Ordinal)
                    && node.RecordsConflicts && conflicts is not null)
                {
                    conflicts.Add(new LocaleConflict(Join(rootNamespace, name), key, node.FileName, existing.FileName, isShared));
                }

                settled[key] = ToEntry(node, key, value, isShared);
            }
        }

        return settled;
    }

    private void AddSection(LocaleSection node, List<string> path, string fileName, string sectionName, bool recordsConflicts)
    {
        var hasShared = node.SharedEntries.Count > 0;
        var hasOwn = node.Entries.Count > 0;

        if (hasShared || hasOwn)
        {
            var name = string.Join('.', path);
            var reference = new LocaleNode(node, fileName, sectionName, recordsConflicts);

            if (hasShared)
            {
                Add(_shared, name, reference);
            }

            if (hasOwn)
            {
                Add(_own, name, reference);
            }
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
            AddSection(section, path, fileName, Join(sectionName, sectionKey), recordsConflicts);
            path.RemoveRange(path.Count - segments.Length, segments.Length);
        }
    }

    private static void Add(Dictionary<string, LocaleNodes> nodes, string name, LocaleNode node)
    {
        ref var named = ref CollectionsMarshal.GetValueRefOrAddDefault(nodes, name, out var exists);

        if (exists)
        {
            (named.Rest ??= []).Add(node);
        }
        else
        {
            named.First = node;
        }
    }

    /// <summary>A section a file holds for a name, with the file it came from.</summary>
    private readonly record struct LocaleNode(LocaleSection Section, string FileName, string SectionName, bool RecordsConflicts);

    /// <summary>The sections given for one name: the first, and a list of the rest from the second on.</summary>
    private struct LocaleNodes
    {
        public LocaleNode First;

        public List<LocaleNode>? Rest;

        public readonly List<LocaleNode> ToList()
            => [First, .. Rest ?? []];
    }
}
