namespace Forma.UI.Features.Composer.Sanitization;

/// <summary>Removes markup a document may not carry.</summary>
public interface IComposerHtmlSanitizer
{
    /// <summary>Returns the content with disallowed markup removed.</summary>
    string Sanitize(string html);
}
