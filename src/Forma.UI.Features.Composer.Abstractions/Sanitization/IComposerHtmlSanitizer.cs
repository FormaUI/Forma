namespace Forma.UI.Features.Composer.Sanitization;

/// <summary>Removes markup a document may not carry.</summary>
public interface IComposerHtmlSanitizer
{
    /// <summary>The markup a document sanitized by this instance may carry.</summary>
    IMarkupPolicy MarkupPolicy { get; }

    /// <summary>Returns the content with disallowed markup removed.</summary>
    string Sanitize(string html);
}
