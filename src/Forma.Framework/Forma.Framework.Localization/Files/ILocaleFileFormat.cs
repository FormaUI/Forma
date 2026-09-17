namespace Forma.Framework.Localization.Files;

/// <summary>A format locale files are written in: the extension its files carry, and how one is read.</summary>
public interface ILocaleFileFormat
{
    /// <summary>The extension a file of this format carries after its culture, with its leading dot.</summary>
    string Extension { get; }

    /// <summary>Reads a file, having checked its metadata lets it be read at all.</summary>
    /// <param name="stream">The file's content.</param>
    /// <param name="fileName">The file's name, named in anything thrown.</param>
    /// <param name="cultureName">The culture the file's name gives it.</param>
    LocaleFile Read(Stream stream, string fileName, string cultureName);
}
