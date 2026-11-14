namespace Forma.Framework.Localization.Files;

/// <summary>One entry a source read for a type, with the file and the section it came from.</summary>
/// <param name="FileName">The name the file has in the source it was read from.</param>
/// <param name="TypeName">The part of the type's name the file's sections completed, dotted, or empty when the entry sits outside any section.</param>
/// <param name="Key">The source text the entry is keyed by.</param>
/// <param name="Value">The text, or null when the text is deliberately the key itself.</param>
public readonly record struct LocaleSourceEntry(string FileName, string TypeName, string Key, string? Value);
