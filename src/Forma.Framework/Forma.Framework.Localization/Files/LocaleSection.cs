namespace Forma.Framework.Localization.Files;

/// <summary>One node of a locale file: the sections beneath it, the entries it holds, and the entries it shares with everything beneath it, each in the order the file writes them.</summary>
/// <param name="Sections">The sections beneath this one, each named for the part of a type's name it completes from where the file sits, or from the assembly's root when its name starts with <see cref="LocalizationSchema.RootedSectionPrefix"/> at the file's top level.</param>
/// <param name="Entries">The translations at this node, keyed by their English source text; a null value says the translation is deliberately the key itself.</param>
/// <param name="SharedEntries">The translations every type reached through this node shares.</param>
public sealed record LocaleSection(IReadOnlyList<KeyValuePair<string, LocaleSection>> Sections, IReadOnlyList<KeyValuePair<string, string?>> Entries, IReadOnlyList<KeyValuePair<string, string?>> SharedEntries)
{
    /// <summary>Creates a node with an empty list of shared entries.</summary>
    /// <param name="sections">The sections beneath this one.</param>
    /// <param name="entries">The translations at this node.</param>
    public LocaleSection(IReadOnlyList<KeyValuePair<string, LocaleSection>> sections, IReadOnlyList<KeyValuePair<string, string?>> entries)
        : this(sections, entries, [])
    {
    }
}
