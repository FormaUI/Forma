namespace Forma.Framework.Localization.Files;

/// <summary>One entry a file declares, with the name of the type it belongs to.</summary>
/// <param name="TypeName">The part of the type's name the file's sections complete, dotted, or empty for the type the file itself is named for.</param>
/// <param name="Key">The source text the entry is keyed by.</param>
/// <param name="Value">The text, or null when the text is deliberately the key itself.</param>
public readonly record struct LocaleFileEntry(string TypeName, string Key, string? Value);
