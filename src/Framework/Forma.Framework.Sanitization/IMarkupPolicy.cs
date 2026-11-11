namespace Forma.Framework.Sanitization;

/// <summary>The markup a document may carry.</summary>
public interface IMarkupPolicy
{
    /// <summary>Whether the subject is permitted, yielding the spelling the policy writes it with.</summary>
    /// <param name="subject">What is being asked about.</param>
    /// <param name="value">The name or address being judged.</param>
    /// <param name="owner">The element owning an attribute.</param>
    /// <param name="canonicalSpelling">The spelling to write; null keeps the one given.</param>
    bool Allows(HtmlSubject subject, ReadOnlySpan<char> value, ReadOnlySpan<char> owner, out string? canonicalSpelling);
}
