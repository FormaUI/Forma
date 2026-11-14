namespace Forma.Framework.Localization.Files;

/// <summary>A locale file as read: its sections and entries.</summary>
/// <param name="Root">The file's top-level node.</param>
public sealed record LocaleFile(LocaleSection Root)
{
    /// <summary>Every entry the file declares, each with the part of a type's name its sections complete, in the order the file writes them.</summary>
    /// <remarks>A shared section's entries are listed under the section's reserved name.</remarks>
    public IReadOnlyList<LocaleFileEntry> GetEntries()
    {
        var entries = new List<LocaleFileEntry>();
        Collect(Root, [], entries);

        return entries;
    }

    /// <summary>The segments a section's name counts from the assembly's root, or null for a section counted from where the file sits.</summary>
    /// <param name="name">The section's name as the file writes it.</param>
    internal static string[]? GetRootedSegments(string name)
        => name.StartsWith(LocalizationSchema.RootedSectionPrefix, StringComparison.Ordinal)
            ? name[LocalizationSchema.RootedSectionPrefix.Length..].Split('.')
            : null;

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
