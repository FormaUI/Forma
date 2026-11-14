namespace Forma.Framework.Localization.Files;

/// <summary>One section a source reads for a type, by the file it sits in and the part of the type's name it completes.</summary>
/// <param name="FileName">The name the file has in the source it was read from.</param>
/// <param name="TypeName">The part of the type's name the file's sections completed, dotted, or empty for the file's top level, ending in the shared section's reserved name where the type reads the section's shared entries.</param>
public readonly record struct LocaleSourceSection(string FileName, string TypeName);
