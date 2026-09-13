namespace Forma.Framework.Localization.Files;

/// <summary>A translation file as read: its sections and entries, and the root it completes a type's name from when it declares one.</summary>
/// <param name="Root">The file's top-level node.</param>
/// <param name="RootNamespace">The namespace the file completes a type's name from, or null when it declares none.</param>
public sealed record LocaleFile(LocaleSection Root, string? RootNamespace)
{
    /// <summary>Every translation the file declares, each with the part of a type's name its sections complete, in the order the file writes them.</summary>
    public IReadOnlyList<LocaleFileEntry> GetEntries()
    {
        var entries = new List<LocaleFileEntry>();
        Collect(Root, [], entries);

        return entries;
    }

    /// <summary>Adds the entries for the type whose name the given segments complete, in the order the file writes them.</summary>
    /// <param name="remainingSegments">The part of the type's name the file's own name leaves out.</param>
    /// <param name="entries">The list the entries are added to.</param>
    /// <param name="fileName">The name the file has in the source it was read from.</param>
    internal void Collect(ReadOnlySpan<string> remainingSegments, List<LocaleSourceEntry> entries, string fileName)
        => Collect(Root, remainingSegments, entries, fileName, string.Empty);

    // A section completes the type's name, whether its key spells several segments or one; the entries are the type's once the name is complete.
    private static void Collect(LocaleSection node, ReadOnlySpan<string> remainingSegments, List<LocaleSourceEntry> entries, string fileName, string typeName)
    {
        foreach (var (name, section) in node.Sections)
        {
            var sectionSegments = name.Split('.');
            if (sectionSegments.Length <= remainingSegments.Length && remainingSegments[..sectionSegments.Length].SequenceEqual(sectionSegments))
            {
                Collect(section, remainingSegments[sectionSegments.Length..], entries, fileName, typeName.Length == 0 ? name : $"{typeName}.{name}");
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
}
