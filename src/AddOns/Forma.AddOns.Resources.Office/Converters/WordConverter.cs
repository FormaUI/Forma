namespace Forma.Framework.Resources.Office.Converters;

using OfficeIMO.Word;
using OfficeIMO.Word.Html;

/// <summary>Converts a Word document into markup.</summary>
internal sealed class WordConverter : IOfficeConverter
{
    // A style block would not survive sanitizing.
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
