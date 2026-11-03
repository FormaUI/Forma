namespace Forma.Tests.Localization;

using Forma.Framework.Localization.Files;

/// <summary>A locale file format that hands back the file registered under the name it is read under.</summary>
public sealed class FakeLocaleFileFormat : ILocaleFileFormat
{
    /// <summary>The files, each keyed by the name it is read under.</summary>
    public Dictionary<string, LocaleFile> Files { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public string Extension => ".fake";

    /// <inheritdoc />
    public LocaleFile Read(Stream stream, string fileName, string cultureName)
        => Files[fileName];
}
