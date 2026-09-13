namespace Forma.Framework.Localization.Files;

/// <summary>What a file's metadata entry declares beyond the fields every file must carry.</summary>
/// <param name="RootNamespace">The namespace the file completes a type's name from, or null to complete it from the assembly's name.</param>
public readonly record struct LocalizationFileMetadata(string? RootNamespace);
