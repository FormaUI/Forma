namespace Forma.Framework.Localization.Files;

/// <summary>One translation a file declares, with the name of the type it belongs to.</summary>
/// <param name="TypeName">The part of the type's name the file's sections complete, dotted, or empty for the type the file itself is named for.</param>
/// <param name="Key">The English source text the translation is keyed by.</param>
/// <param name="Value">The translation, or null when the translation is deliberately the key itself.</param>
public readonly record struct LocaleFileEntry(string TypeName, string Key, string? Value);
