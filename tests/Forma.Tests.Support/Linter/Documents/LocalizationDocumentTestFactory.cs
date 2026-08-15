namespace Forma.Tests.Linter.Documents;

using System.Text;

using Forma.Framework.Localization.Linter.Documents;

/// <summary>Builds documents out of file text, so a test can lint without touching disk.</summary>
public static class LocalizationDocumentTestFactory
{
    /// <summary>The document the content parses to.</summary>
    /// <param name="name">The file name the document carries.</param>
    /// <param name="content">The file's text.</param>
    public static LocalizationDocument Read(string name, string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(content);

        return LocalizationDocument.TryRead(name, Encoding.UTF8.GetBytes(content), out var document, out var error)
            ? document!
            : throw new InvalidOperationException($"'{name}' does not parse as a localization file: {error}");
    }
}
