namespace Forma.Framework.Composer.Documents;

using System.Diagnostics.CodeAnalysis;

/// <summary>A document format the composer can hold or convert.</summary>
public sealed record ComposerDocumentFormat
{
    /// <summary>Creates a format from its identifier, which is compared case-insensitively.</summary>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    public ComposerDocumentFormat(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id.ToLowerInvariant();
    }

    /// <summary>The format's lowercase identifier.</summary>
    public string Id { get; }

    /// <summary>HTML, the format the rich text editor reads and writes.</summary>
    public static ComposerDocumentFormat Html { get; } = new("html");

    /// <summary>Markdown source.</summary>
    public static ComposerDocumentFormat Markdown { get; } = new("markdown");

    /// <summary>Unformatted text, a conversion target with no editor of its own.</summary>
    public static ComposerDocumentFormat PlainText { get; } = new("text");

    /// <summary>The serialized widget tree the page designer edits.</summary>
    public static ComposerDocumentFormat WidgetTree { get; } = new("widget-tree");

    /// <inheritdoc />
    public override string ToString()
        => Id;
}
