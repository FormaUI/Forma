namespace Forma.Blazor.Components.Files;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A resource a provider converted to markup, such as a document rendered to HTML. The provider
/// must return sanitised markup, because a viewer renders it as trusted HTML.
/// </summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record MarkupResourceInfo : ResourceInfo
{
    public MarkupResourceInfo(string source, string html)
        : base(source, ResourceKind.Document)
    {
        ArgumentNullException.ThrowIfNull(html);

        Html = html;
    }

    /// <summary>The sanitised markup the provider produced.</summary>
    public string Html { get; }
}
