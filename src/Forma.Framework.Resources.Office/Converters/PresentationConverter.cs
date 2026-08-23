namespace Forma.Framework.Resources.Office.Converters;

using System.Net;
using System.Text;

using Forma.Framework.Localization;

using OfficeIMO.PowerPoint;

/// <summary>Projects a presentation into one section per slide, since OfficeIMO ships no HTML converter for it.</summary>
internal sealed class PresentationConverter(IStringLocale locale) : IOfficeConverter
{
    // A deck this long is already beyond what a preview pane can usefully show.
    private const int MaxSlides = 200;

    /// <inheritdoc />
    public string Extension
        => ".pptx";

    /// <inheritdoc />
    public async ValueTask<string> ConvertAsync(Stream content, CancellationToken cancellationToken)
    {
        using var presentation = await PowerPointPresentation.LoadAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);

        var markup = new StringBuilder();
        var number = 0;

        foreach (var slide in presentation.Slides)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (number == MaxSlides)
            {
                markup.Append("<p>").Append(WebUtility.HtmlEncode(locale["Showing the first {MaxSlides} slides.", MaxSlides].Value)).Append("</p>");

                break;
            }

            AppendSlide(markup, slide, ++number);
        }

        return markup.ToString();
    }

    private static bool IsTitle(PowerPointTextBox textBox)
        => textBox.IsPlaceholder
            && textBox.PlaceholderType is PowerPointPlaceholderType.Title or PowerPointPlaceholderType.CenteredTitle;

    private void AppendSlide(StringBuilder markup, PowerPointSlide slide, int number)
    {
        markup.Append("<section>");

        string? title = null;
        var body = new List<string>();

        foreach (var textBox in slide.TextBoxes)
        {
            var text = textBox.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (title is null && IsTitle(textBox))
            {
                title = text;
            }
            else
            {
                body.Add(text);
            }
        }

        // A layout leaves a title placeholder behind whether or not the author filled it, so a blank one still needs naming.
        markup.Append("<h2>")
            .Append(WebUtility.HtmlEncode(title ?? locale["Slide {Number}", number].Value))
            .Append("</h2>");

        foreach (var text in body)
        {
            markup.Append("<p>").Append(WebUtility.HtmlEncode(text)).Append("</p>");
        }

        if (slide.HasSpeakerNotes && slide.Notes?.Text is { } notes && !string.IsNullOrWhiteSpace(notes))
        {
            markup.Append("<blockquote>").Append(WebUtility.HtmlEncode(notes)).Append("</blockquote>");
        }

        markup.Append("</section>");
    }
}
