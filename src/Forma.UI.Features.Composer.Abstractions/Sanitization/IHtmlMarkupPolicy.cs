namespace Forma.UI.Features.Composer.Sanitization;

/// <summary>The markup a document may carry.</summary>
public interface IHtmlMarkupPolicy
{
    /// <summary>Whether the subject is permitted, yielding the spelling the policy writes it with, or null to keep the one given.</summary>
    /// <param name="subject">What is being asked about.</param>
    /// <param name="value">The name or address being judged.</param>
    /// <param name="owner">The element owning an attribute, and empty for every other subject.</param>
    /// <param name="canonical">The spelling to write, or null where the policy normalizes nothing.</param>
    bool Allows(HtmlSubject subject, ReadOnlySpan<char> value, ReadOnlySpan<char> owner, out string? canonical);
}
