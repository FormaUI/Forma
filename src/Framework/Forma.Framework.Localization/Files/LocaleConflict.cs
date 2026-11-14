namespace Forma.Framework.Localization.Files;

/// <summary>An entry two files of one source give different texts, and the file whose text the source keeps.</summary>
/// <param name="TypeName">The full name of the type, or of the namespace a shared entry is for.</param>
/// <param name="Key">The English source text the entry is keyed by.</param>
/// <param name="FileName">The file whose text the source keeps.</param>
/// <param name="OtherFileName">The file whose text the source sets aside.</param>
public readonly record struct LocaleConflict(string TypeName, string Key, string FileName, string OtherFileName);
