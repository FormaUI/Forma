namespace Forma.Framework.Localization.Files;

/// <summary>A translation file as read: its sections and entries, and the root it completes a type's name from when it declares one.</summary>
/// <param name="Root">The file's top-level node.</param>
/// <param name="RootNamespace">The namespace the file completes a type's name from, or null when it declares none.</param>
public sealed record LocaleFile(LocaleSection Root, string? RootNamespace)
{
    /// <summary>Every translation the file declares, each with the part of a type's name its sections complete, in the order the file writes them; a shared section is listed under its own reserved name.</summary>
    public IReadOnlyList<LocaleFileEntry> GetEntries()
    {
        var entries = new List<LocaleFileEntry>();
        Collect(Root, [], entries);

        return entries;
    }

    /// <summary>Adds the entries for the type whose name the given segments complete, the shared ones of every node on the way first, in the order the file writes them.</summary>
    /// <param name="remainingSegments">The part of the type's name the file's own name leaves out.</param>
    /// <param name="entries">The list the entries are added to.</param>
    /// <param name="fileName">The name the file has in the source it was read from.</param>
    internal void Collect(ReadOnlySpan<string> remainingSegments, List<LocaleSourceEntry> entries, string fileName)
        => Collect(Root, remainingSegments, entries, fileName, string.Empty);

    // A node's shared entries belong to every type reached through it and come first wherever the file writes them, so an entry of the type's own overrides one; a section completes the type's name, whether its key spells several segments or one; the entries are the type's once the name is complete.
    private static void Collect(LocaleSection node, ReadOnlySpan<string> remainingSegments, List<LocaleSourceEntry> entries, string fileName, string typeName)
    {
        foreach (var (name, section) in node.Sections)
        {
            if (name == LocalizationSchema.SharedKey)
            {
                var sharedName = Join(typeName, name);
                foreach (var (key, value) in section.Entries)
                {
                    entries.Add(new(fileName, sharedName, key, value));
                }
            }
        }

        foreach (var (name, section) in node.Sections)
        {
            if (name == LocalizationSchema.SharedKey)
            {
                continue;
            }

            var sectionSegments = name.Split('.');
            if (sectionSegments.Length <= remainingSegments.Length && remainingSegments[..sectionSegments.Length].SequenceEqual(sectionSegments))
            {
                Collect(section, remainingSegments[sectionSegments.Length..], entries, fileName, Join(typeName, name));
            }
        }

        if (remainingSegments.Length == 0)
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
