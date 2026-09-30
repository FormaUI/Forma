namespace Forma.Framework.Sanitization;

/// <summary>Sanitizes a document to the markup its policy permits.</summary>
public interface IHtmlSanitizer
{
    /// <summary>The markup a document sanitized by this instance may carry.</summary>
    IMarkupPolicy MarkupPolicy { get; }

    /// <summary>Returns <paramref name="html"/> with the markup its policy refuses removed.</summary>
    string Sanitize(string html);
}
