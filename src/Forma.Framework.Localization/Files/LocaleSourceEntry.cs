namespace Forma.Framework.Localization.Files;

/// <summary>One translation a source read for a type, with the file and the section it came from.</summary>
/// <param name="FileName">The name the file has in the source it was read from.</param>
/// <param name="TypeName">The part of the type's name the file's sections completed, dotted, or empty when the entry sits outside any section.</param>
/// <param name="Key">The English source text the translation is keyed by.</param>
/// <param name="Value">The translation, or null when the translation is deliberately the key itself.</param>
public readonly record struct LocaleSourceEntry(string FileName, string TypeName, string Key, string? Value);
