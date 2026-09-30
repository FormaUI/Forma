namespace Forma.Framework.Composer.Documents;

using System.Diagnostics.CodeAnalysis;

/// <summary>A document format the composer can hold or convert.</summary>
public sealed record ComposerDocumentFormat
{
    /// <summary>Creates a format from its identifier.</summary>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    public ComposerDocumentFormat(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id.ToLowerInvariant();
    }

    /// <summary>The format's lowercase identifier.</summary>
    public string Id { get; }

    /// <summary>The HTML format.</summary>
    public static ComposerDocumentFormat Html { get; } = new("html");

    /// <summary>The Markdown format.</summary>
    public static ComposerDocumentFormat Markdown { get; } = new("markdown");

    /// <summary>The plain text format.</summary>
    public static ComposerDocumentFormat PlainText { get; } = new("text");

    /// <summary>The serialized widget tree format.</summary>
    internal static ComposerDocumentFormat WidgetTree { get; } = new("widget-tree");

    /// <inheritdoc />
    public override string ToString()
        => Id;
}
