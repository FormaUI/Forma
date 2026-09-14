namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A resource a provider converted to markup, such as a document rendered to HTML. The provider
/// must return sanitized markup, because a viewer renders it as trusted HTML.
/// </summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record MarkupResourceInfo : DocumentResourceInfo
{
    /// <summary>Creates a markup resource from its source address and its sanitized HTML.</summary>
    public MarkupResourceInfo(string source, string html)
        : base(source)
    {
        ArgumentNullException.ThrowIfNull(html);

        Html = html;
    }

    /// <summary>The sanitized markup the provider produced.</summary>
    public string Html { get; }
}
