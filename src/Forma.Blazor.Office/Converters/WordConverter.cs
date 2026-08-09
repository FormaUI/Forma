namespace Forma.Blazor.Office.Converters;

using OfficeIMO.Word;
using OfficeIMO.Word.Html;

/// <summary>Converts a Word document through the converter OfficeIMO ships for it.</summary>
internal sealed class WordConverter : IOfficeConverter
{
    // A style block would not survive sanitising, so emitting one only inflates the markup.
    private static readonly WordToHtmlOptions ConversionOptions = new()
    {
        EmbedImagesAsBase64 = true,
        IncludeDefaultCss = false,
    };

    /// <inheritdoc />
    public string Extension
        => ".docx";

    /// <inheritdoc />
    public async ValueTask<string> ConvertAsync(Stream content, CancellationToken cancellationToken)
    {
        using var document = await WordDocument.LoadAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);

        return document.ToHtml(ConversionOptions);
    }
}
