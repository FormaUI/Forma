namespace Forma.Framework.Composer.Documents;

using Forma.Framework.Resources;

/// <summary>Document content together with the format it is written in, and the files carried beside it.</summary>
public sealed record ComposerDocument(ComposerDocumentFormat Format, string Content)
{
    /// <summary>The files the document carries beside its content, as an email carries its attachments.</summary>
    public IReadOnlyList<ResourceInfo> Attachments { get; init; } = [];
}
