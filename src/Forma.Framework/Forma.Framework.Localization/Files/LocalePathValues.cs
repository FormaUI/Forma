namespace Forma.Framework.Localization.Files;

/// <summary>What a locale path's tokens stand for in one source and one culture, apart from the anchor.</summary>
/// <param name="AssemblyName">The name of the assembly the type lives in.</param>
/// <param name="RootNamespace">The root namespace.</param>
/// <param name="RootDir">Where the source's names begin, written with <c>/</c> between folders, or empty when the source has no known root.</param>
/// <param name="LocalesDir">The name of a folder of locale files, written with <c>/</c> between folders.</param>
/// <param name="Culture">The culture the file is for.</param>
/// <param name="Extension">The extension the backend's files carry, with its leading dot.</param>
public readonly record struct LocalePathValues(
    string AssemblyName,
    string RootNamespace,
    string RootDir,
    string LocalesDir,
    string Culture,
    string Extension);
