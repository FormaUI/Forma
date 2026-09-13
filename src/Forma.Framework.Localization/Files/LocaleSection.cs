namespace Forma.Framework.Localization.Files;

/// <summary>One node of a translation file: the sections beneath it and the entries it holds, each in the order the file writes them.</summary>
/// <param name="Sections">The sections beneath this one, each named for the part of a type's name it completes.</param>
/// <param name="Entries">The translations at this node, keyed by their English source text; a null value says the translation is deliberately the key itself.</param>
public sealed record LocaleSection(IReadOnlyList<KeyValuePair<string, LocaleSection>> Sections, IReadOnlyList<KeyValuePair<string, string?>> Entries);
