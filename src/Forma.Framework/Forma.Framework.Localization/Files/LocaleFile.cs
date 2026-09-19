namespace Forma.Framework.Localization.Files;

/// <summary>A locale file as read: its sections and entries.</summary>
/// <param name="Root">The file's top-level node.</param>
public sealed record LocaleFile(LocaleSection Root)
{
    /// <summary>Every translation the file declares, each with the part of a type's name its sections complete, in the order the file writes them; a shared section is listed under its own reserved name.</summary>
    public IReadOnlyList<LocaleFileEntry> GetEntries()
    {
        var entries = new List<LocaleFileEntry>();
        Collect(Root, [], entries);

        return entries;
    }

    /// <summary>Adds the entries for a type from where the file sits: the shared ones of every node on the way first, in the order the file writes them, through the sections completing the type's name from the file's own position.</summary>
    /// <param name="nameSegments">The type's name below the assembly's root, or its full name for a type outside the root namespace.</param>
    /// <param name="depth">How many of the segments the file's own position spells.</param>
    /// <param name="entries">The list the entries are added to.</param>
    /// <param name="fileName">The name the file has in the source it was read from.</param>
    internal void Collect(ReadOnlySpan<string> nameSegments, int depth, List<LocaleSourceEntry> entries, string fileName)
        => Collect(Root, nameSegments, depth, entries, fileName, string.Empty);

    /// <summary>Adds the entries for a type from a rooted section: the top-level section naming the type's first segments from the root, whatever the file's own position.</summary>
    /// <param name="nameSegments">The type's name below the assembly's root, or its full name for a type outside the root namespace.</param>
    /// <param name="depth">How many of the segments the section names.</param>
    /// <param name="entries">The list the entries are added to.</param>
    /// <param name="fileName">The name the file has in the source it was read from.</param>
    internal void CollectRooted(ReadOnlySpan<string> nameSegments, int depth, List<LocaleSourceEntry> entries, string fileName)
    {
        foreach (var (name, section) in Root.Sections)
        {
            if (GetRootedSegments(name) is { } segments && segments.Length == depth && nameSegments[..depth].SequenceEqual(segments))
            {
                Collect(section, nameSegments, depth, entries, fileName, name);
            }
        }
    }

    /// <summary>The segments a section's name counts from the assembly's root, or null for a section counted from where the file sits.</summary>
    /// <param name="name">The section's name as the file writes it.</param>
    internal static string[]? GetRootedSegments(string name)
        => name.StartsWith(LocalizationSchema.RootedSectionPrefix, StringComparison.Ordinal)
            ? name[LocalizationSchema.RootedSectionPrefix.Length..].Split('.')
            : null;

    // A node's shared entries belong to every type reached through it and come first, so an entry of the type's own overrides one; a section completes the type's name, whether its key spells several segments or one, and a rooted one is left to its own collection; the entries are the type's once the name is complete.
    private static void Collect(LocaleSection node, ReadOnlySpan<string> nameSegments, int depth, List<LocaleSourceEntry> entries, string fileName, string typeName)
    {
        var sharedName = Join(typeName, LocalizationSchema.SharedKey);
        foreach (var (key, value) in node.SharedEntries)
        {
            entries.Add(new(fileName, sharedName, key, value));
        }

        foreach (var (name, section) in node.Sections)
        {
            if (GetRootedSegments(name) is not null)
            {
                continue;
            }

            var sectionSegments = name.Split('.');
            if (depth + sectionSegments.Length <= nameSegments.Length && nameSegments.Slice(depth, sectionSegments.Length).SequenceEqual(sectionSegments))
            {
                Collect(section, nameSegments, depth + sectionSegments.Length, entries, fileName, Join(typeName, name));
            }
        }

        if (depth == nameSegments.Length)
        {
            foreach (var (key, value) in node.Entries)
            {
                entries.Add(new(fileName, typeName, key, value));
            }
        }
    }

    private static void Collect(LocaleSection node, List<string> typeSegments, List<LocaleFileEntry> entries)
    {
        var typeName = string.Join('.', typeSegments);
        foreach (var (key, value) in node.Entries)
        {
            entries.Add(new(typeName, key, value));
        }

        var sharedName = Join(typeName, LocalizationSchema.SharedKey);
        foreach (var (key, value) in node.SharedEntries)
        {
            entries.Add(new(sharedName, key, value));
        }

        foreach (var (name, section) in node.Sections)
        {
            typeSegments.Add(name);
            Collect(section, typeSegments, entries);
            typeSegments.RemoveAt(typeSegments.Count - 1);
        }
    }

    private static string Join(string typeName, string name)
        => typeName.Length == 0 ? name : $"{typeName}.{name}";
}
