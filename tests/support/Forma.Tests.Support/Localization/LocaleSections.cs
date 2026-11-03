namespace Forma.Tests.Localization;

using Forma.Framework.Localization.Files;

/// <summary>Builds the nodes of a locale file from entries and sections given inline.</summary>
public static class LocaleSections
{
    /// <summary>A node holding the given entries and no sections.</summary>
    public static LocaleSection CreateEntries(params (string Key, string? Value)[] entries)
        => new([], entries.Select(entry => new KeyValuePair<string, string?>(entry.Key, entry.Value)).ToList());

    /// <summary>A node holding the given sections and no entries of its own.</summary>
    public static LocaleSection CreateSections(params (string Name, LocaleSection Section)[] sections)
        => new(sections.Select(section => new KeyValuePair<string, LocaleSection>(section.Name, section.Section)).ToList(), []);

    /// <summary>The node with the given entries shared with everything beneath it.</summary>
    public static LocaleSection WithShared(this LocaleSection node, params (string Key, string? Value)[] shared)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node with { SharedEntries = shared.Select(entry => new KeyValuePair<string, string?>(entry.Key, entry.Value)).ToList() };
    }
}
