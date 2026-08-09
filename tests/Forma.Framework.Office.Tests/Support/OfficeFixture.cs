namespace Forma.Framework.Office.Tests.Support;

using OfficeIMO.Excel;
using OfficeIMO.PowerPoint;
using OfficeIMO.Word;

/// <summary>Builds real Office documents in memory, so conversion is exercised against the actual formats.</summary>
public static class OfficeFixture
{
    /// <summary>The bytes of a Word document composed by <paramref name="compose"/>.</summary>
    public static byte[] Docx(Action<WordDocument> compose)
        => Build(".docx", compose, path => WordDocument.Create(path), document => document.Save());

    /// <summary>The bytes of a workbook composed by <paramref name="compose"/>.</summary>
    public static byte[] Xlsx(Action<ExcelDocument> compose)
        => Build(".xlsx", compose, path => ExcelDocument.Create(path), workbook => workbook.Save());

    /// <summary>The bytes of a presentation composed by <paramref name="compose"/>.</summary>
    public static byte[] Pptx(Action<PowerPointPresentation> compose)
        => Build(".pptx", compose, path => PowerPointPresentation.Create(path), presentation => presentation.Save());

    private static byte[] Build<TDocument>(string extension, Action<TDocument> compose, Func<string, TDocument> create, Action<TDocument> save)
        where TDocument : IDisposable
    {
        ArgumentNullException.ThrowIfNull(compose);

        var path = Path.Combine(Path.GetTempPath(), $"forma-office-{Guid.NewGuid():N}{extension}");

        try
        {
            using (var document = create(path))
            {
                compose(document);

                save(document);
            }

            return File.ReadAllBytes(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
